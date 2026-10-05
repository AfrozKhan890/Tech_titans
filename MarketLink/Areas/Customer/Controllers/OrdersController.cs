using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MarketLink.Repositories;
using MarketLink.Services;
using MarketLink.Models.Enums;
using MarketLink.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
public class OrdersController : MarketLink.Services.CustomerAreaController
{
    private readonly IOrderService _orderService;
    private readonly ICartService _cartService;
    private readonly IUnitOfWork _unitOfWork;

    public OrdersController(IOrderService orderService, ICartService cartService, IUnitOfWork unitOfWork)
    {
        _orderService = orderService;
        _cartService = cartService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index(string? statusFilter)
    {
        ViewData["Title"] = "My Orders";
        var customerId = CustomerId;
        var orders = await _orderService.GetCustomerOrdersAsync(customerId);

        if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<OrderStatus>(statusFilter, true, out var parsedStatus))
        {
            orders = orders.Where(o => o.Status == parsedStatus).ToList();
        }

        ViewBag.StatusFilter = statusFilter ?? "";
        return View(orders);
    }

    public async Task<IActionResult> History(string? statusFilter, string? q)
    {
        ViewData["Title"] = "Order History";
        var customerId = CustomerId;

        var query = _unitOfWork.Repository<Order>().Query()
            .Include(o => o.Farmer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<OrderStatus>(statusFilter, true, out var parsedStatus))
        {
            query = query.Where(o => o.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim();
            query = query.Where(o => o.OrderNumber.Contains(search) ||
                                     o.Farmer.FarmName.Contains(search) ||
                                     o.Items.Any(i => i.ProductNameSnapshot.Contains(search)));
        }

        var orders = await query.OrderByDescending(o => o.OrderedAt).ToListAsync();

        ViewBag.StatusFilter = statusFilter;
        ViewBag.Query = q;
        return View(orders);
    }

    public async Task<IActionResult> Details(int id)
    {


        var order = await _unitOfWork.Repository<Order>().Query()
            .Include(o => o.Farmer).ThenInclude(f => f.User)
            .Include(o => o.Market)
            .Include(o => o.PickupSlot)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == CustomerId);

        if (order == null) return NotFound();

        ViewBag.PickupSlots = await _unitOfWork.Repository<PickupSlot>().Query()
            .Include(s => s.Market)
            .Where(s => s.FarmerId == order.FarmerId && s.IsActive)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToListAsync();
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(int id)
    {
        var customerId = CustomerId;
        var order = await _unitOfWork.Repository<Order>().Query()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == customerId);

        if (order == null)
        {
            TempData["Error"] = "Order not found.";
            return RedirectToAction(nameof(History));
        }

        int addedCount = 0;
        foreach (var item in order.Items)
        {
            try
            {
                await _cartService.AddToCartAsync(customerId, item.ProductId, item.QuantityKg);
                addedCount++;
            }
            catch
            {

            }
        }

        if (addedCount > 0)
        {
            TempData["Success"] = $"Added {addedCount} item(s) from Order #{order.OrderNumber} to your basket!";
            return RedirectToAction("Index", "Cart");
        }
        else
        {
            TempData["Error"] = "Items from this order are currently out of stock or unavailable.";
            return RedirectToAction(nameof(History));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(List<MarketLink.Models.CheckoutPickup> pickups, string? notes)
    {
        try
        {
            var customerId = CustomerId;
            var cartItems = await _cartService.GetCartAsync(customerId);
            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index", "Cart");
            }

            var selections = new List<PickupSelection>();
            foreach (var pickup in (pickups ?? []).Where(p => p != null && p.FarmerId > 0 && p.PickupSlotId > 0))
            {
                var pickupTime = pickup.PickupTime == default
                    ? await GetNextPickupTimeAsync(pickup.PickupSlotId)
                    : pickup.PickupTime;
                selections.Add(new PickupSelection(pickup.FarmerId, pickup.PickupSlotId, pickupTime));
            }

            var orders = await _orderService.PlaceOrderAsync(customerId, cartItems, selections, notes);

            TempData["Success"] = orders.Count == 1
                ? $"Order #{orders[0].OrderNumber} placed successfully!"
                : $"{orders.Count} orders placed successfully! Track them under My Orders.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Index", "Cart");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Modify(int id, int pickupSlotId, DateTime pickupTime, List<ModifyOrderItemRequest> items)
    {
        try
        {
            if (items == null || items.Count == 0)
                throw new InvalidOperationException("At least one order item is required.");

            var modified = await _orderService.ModifyOrderAsync(
                id,
                CustomerId,
                pickupSlotId,
                pickupTime,
                items.Select(i => new OrderItemModification(i.ProductId, i.QuantityKg)).ToList());

            TempData[modified ? "Success" : "Error"] = modified
                ? "Order updated successfully."
                : "This order can no longer be modified.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            var cancelled = await _orderService.CancelOrderAsync(id, "Customer request", "Customer", actorCustomerId: CustomerId);
            TempData[cancelled ? "Success" : "Error"] = cancelled
                ? "Order cancelled successfully."
                : "This order can no longer be cancelled.";
        }
        catch(Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<DateTime> GetNextPickupTimeAsync(int pickupSlotId)
    {
        var slot = await _unitOfWork.Repository<MarketLink.Models.PickupSlot>()
            .Query()
            .FirstOrDefaultAsync(s => s.Id == pickupSlotId && s.IsActive);

        if (slot == null)
            throw new InvalidOperationException("The selected pickup slot is no longer available.");

        var now = DateTime.Now;
        var daysAhead = ((int)slot.DayOfWeek - (int)now.DayOfWeek + 7) % 7;
        var candidate = now.Date.AddDays(daysAhead).Add(slot.StartTime.ToTimeSpan());

        if (candidate <= now)
            candidate = candidate.AddDays(7);

        return candidate;
    }
}

public class ModifyOrderItemRequest
{
    public int ProductId { get; set; }
    public int QuantityKg { get; set; }
}
