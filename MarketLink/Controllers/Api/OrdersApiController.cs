using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MarketLink.Models;
using MarketLink.Repositories;
using MarketLink.Services;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MarketLink.Controllers.Api;

[ApiController]
[Authorize(Roles = "Customer,Admin")]
[Route("api/orders")]
[Produces("application/json")]
public class OrdersApiController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderService _orderService;

    public OrdersApiController(IUnitOfWork unitOfWork, IOrderService orderService)
    {
        _unitOfWork = unitOfWork;
        _orderService = orderService;
    }

    private Task<int?> CurrentCustomerIdAsync()
        => ActorResolver.ResolveCustomerIdAsync(User, _unitOfWork, HttpContext);

    private bool IsAdmin => User.IsInRole("Admin");

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyOrders()
    {
        var customerId = await CurrentCustomerIdAsync();
        if (customerId == null) return NotFound(new { success = false, message = "Customer profile not found for this account." });

        return Ok(Project(await _unitOfWork.Repository<Order>().Query()
            .Include(o => o.Farmer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderedAt)
            .ToListAsync()));
    }

    [HttpGet("customer/{customerId:int}")]
    public async Task<IActionResult> GetCustomerOrders(int customerId)
    {
        if (!IsAdmin) return Forbid();

        var orders = await _unitOfWork.Repository<Order>().Query()
            .Include(o => o.Farmer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderedAt)
            .ToListAsync();

        return Ok(Project(orders));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var order = await _orderService.GetOrderAsync(id);
        if (order == null) return NotFound(new { success = false, message = "Order not found." });

        var customerId = await CurrentCustomerIdAsync();
        if (!IsAdmin && order.CustomerId != customerId) return Forbid();

        return Ok(new
        {
            success = true,
            order = new
            {
                order.Id,
                order.OrderNumber,
                order.Status,
                order.TotalAmount,
                order.SubTotal,
                order.Notes,
                order.PaymentMethod,
                order.OrderedAt,
                order.PickupTime,
                order.CompletedAt,
                Customer = new { order.Customer.Id, CustomerName = $"{order.Customer.User.FirstName} {order.Customer.User.LastName}".Trim(), order.Customer.User.PhoneNumber },
                Farmer = new { order.Farmer.Id, order.Farmer.FarmName, order.Farmer.StallNumber },
                Items = order.Items.Select(i => new
                {
                    i.ProductId,
                    i.ProductNameSnapshot,
                    i.QuantityKg,
                    i.PricePerKgSnapshot,
                    i.TotalPrice
                })
            }
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOrder([FromBody] ApiCreateOrderRequest request)
    {
        if (!ModelState.IsValid || !request.Items.Any())
            return BadRequest(new { success = false, message = "Order items are required." });

        var customerId = await CurrentCustomerIdAsync();
        if (customerId == null) return BadRequest(new { success = false, message = "Customer profile not found for this account." });

        var slot = await _unitOfWork.Repository<PickupSlot>().Query()
            .FirstOrDefaultAsync(s => s.Id == request.PickupSlotId && s.IsActive);
        if (slot == null)
            return BadRequest(new { success = false, message = "PickupSlotId must reference an active pickup slot." });

        try
        {
            var cartItems = request.Items.Select(i => new CartItem
            {
                CustomerId = customerId.Value,
                ProductId = i.ProductId,
                QuantityKg = i.QuantityKg
            }).ToList();

            var pickupTime = request.PickupTime ?? GetNextPickupTime(slot);
            var selections = new List<PickupSelection> { new(slot.FarmerId, slot.Id, pickupTime) };
            var orders = await _orderService.PlaceOrderAsync(customerId.Value, cartItems, selections, request.Notes);
            var order = orders[0];

            return StatusCode(201, new
            {
                success = true,
                message = "Order placed successfully.",
                orderId = order.Id,
                orderNumber = order.OrderNumber,
                totalAmount = order.TotalAmount
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    private static DateTime GetNextPickupTime(PickupSlot slot)
    {
        var now = DateTime.Now;
        var daysAhead = ((int)slot.DayOfWeek - (int)now.DayOfWeek + 7) % 7;
        var candidate = now.Date.AddDays(daysAhead).Add(slot.StartTime.ToTimeSpan());
        if (candidate <= now) candidate = candidate.AddDays(7);
        return candidate;
    }

    private static object Project(List<Order> orders) => new
    {
        success = true,
        count = orders.Count,
        orders = orders.Select(o => new
        {
            o.Id,
            o.OrderNumber,
            o.Status,
            o.TotalAmount,
            o.PaymentMethod,
            o.OrderedAt,
            o.CompletedAt,
            Farmer = new { o.Farmer.Id, o.Farmer.FarmName },
            Items = o.Items.Select(i => new
            {
                i.ProductId,
                i.ProductNameSnapshot,
                i.QuantityKg,
                i.PricePerKgSnapshot,
                i.TotalPrice
            })
        })
    };
}

public class ApiCreateOrderRequest
{
    public int PickupSlotId { get; set; }
    public DateTime? PickupTime { get; set; }
    public string? Notes { get; set; }
    [Required]
    public List<ApiOrderItemRequest> Items { get; set; } = [];
}

public class ApiOrderItemRequest
{
    public int ProductId { get; set; }
    public int QuantityKg { get; set; }
}
