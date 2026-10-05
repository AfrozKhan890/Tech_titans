using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MarketLink.Repositories;
using MarketLink.Services;
using MarketLink.Models;
using MarketLink.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarketLink.Areas.Farmer.Controllers;

[Area("Farmer")]
[Authorize(Roles = "Farmer")]
public class OrdersController : MarketLink.Services.FarmerAreaController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderService _orderService;

    public OrdersController(IUnitOfWork unitOfWork, IOrderService orderService)
    {
        _unitOfWork = unitOfWork;
        _orderService = orderService;
    }

    public async Task<IActionResult> Index(string? status)
    {
        ViewData["Title"] = "Customer Orders";
        int farmerId = FarmerId;

        OrderStatus? filterStatus = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, true, out var parsed))
        {
            filterStatus = parsed;
        }

        var orders = await _unitOfWork.Repository<Order>().Query()
            .Include(o => o.Customer)
                .ThenInclude(c => c.User)
            .Include(o => o.PickupSlot)
                .ThenInclude(ps => ps!.Market)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Where(o => o.FarmerId == farmerId)
            .Where(o => !filterStatus.HasValue || o.Status == filterStatus.Value)
            .OrderByDescending(o => o.OrderedAt)
            .ToListAsync();

        ViewBag.CurrentStatus = status;
        ViewBag.PendingCount = await _unitOfWork.Repository<Order>()
            .CountAsync(o => o.FarmerId == farmerId && o.Status == OrderStatus.Pending);

        return View(orders);
    }

    public async Task<IActionResult> Details(int id)
    {
        int farmerId = FarmerId;

        var order = await _unitOfWork.Repository<Order>().Query()
            .Include(o => o.Customer)
                .ThenInclude(c => c.User)
            .Include(o => o.PickupSlot)
                .ThenInclude(ps => ps!.Market)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.FarmerId == farmerId);

        if (order == null) return NotFound();

        ViewData["Title"] = $"Order #{order.OrderNumber}";
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(int id)
    {

        var success = await _orderService.AcceptOrderAsync(id, FarmerId);
        TempData[success ? "Success" : "Error"] = success
            ? "Order accepted. The customer has been notified."
            : "This order is no longer waiting for a response.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkReady(int id)
    {
        var success = await _orderService.UpdateOrderStatusAsync(id, OrderStatus.ReadyForPickup, "Order prepared and packed.", FarmerId);
        TempData[success ? "Success" : "Error"] = success
            ? "Order marked as ready for pickup. The customer has been notified."
            : "Only an accepted order can be marked ready.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var success = await _orderService.UpdateOrderStatusAsync(id, OrderStatus.Completed, "Picked up and paid physically.", FarmerId);
        TempData[success ? "Success" : "Error"] = success
            ? "Order completed. Payment was collected at pickup."
            : "This order can no longer be completed.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? reason)
    {
        var success = await _orderService.CancelOrderAsync(id, reason ?? "Unavailable", "Farmer", FarmerId);
        TempData[success ? "Success" : "Error"] = success
            ? "Order cancelled and stock returned to your listings."
            : "This order can no longer be cancelled.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
