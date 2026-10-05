using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MarketLink.Models.Enums;
using MarketLink.Repositories;
using MarketLink.Services;
using MarketLink.Models;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Areas.Farmer.Controllers;

[Area("Farmer")]
[Authorize(Roles = "Farmer")]
public class DashboardController : MarketLink.Services.FarmerAreaController
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Farmer Dashboard";
        var farmer = await GetFarmerAsync();
        if (farmer is null) return MissingProfileResult(Request.Path);

        var products = await _unitOfWork.Repository<Product>().Query()
            .Where(p => p.FarmerId == FarmerId)
            .ToListAsync();

        var orders = await _unitOfWork.Repository<Order>().Query()
            .Include(o => o.Customer).ThenInclude(c => c.User)
            .Include(o => o.Items)
            .Where(o => o.FarmerId == FarmerId)
            .OrderByDescending(o => o.OrderedAt)
            .ToListAsync();

        // ---- Extra analytics for the redesigned dashboard ----
        var today = DateTime.UtcNow.Date;
        var live = orders.Where(o => o.Status != OrderStatus.Cancelled).ToList();
        var completedOrders = orders.Where(o => o.Status == OrderStatus.Completed).ToList();
        ViewBag.FarmName = farmer.FarmName;
        ViewBag.OrderCount = orders.Count;
        ViewBag.AvgOrderValue = live.Count == 0 ? 0m : live.Average(o => o.TotalAmount);
        ViewBag.FulfilmentRate = orders.Count == 0 ? 0.0 : Math.Round(completedOrders.Count * 100.0 / orders.Count, 1);

        decimal RevenueBetween(DateTime from, DateTime to) =>
            live.Where(o => o.OrderedAt >= from && o.OrderedAt < to).Sum(o => o.TotalAmount);
        var thisWeek = RevenueBetween(today.AddDays(-6), today.AddDays(1));
        var lastWeek = RevenueBetween(today.AddDays(-13), today.AddDays(-6));
        ViewBag.WeekRevenue = thisWeek;
        ViewBag.RevenueTrend = lastWeek == 0 ? (thisWeek > 0 ? 100.0 : 0.0) : Math.Round((double)((thisWeek - lastWeek) / lastWeek * 100m), 1);

        var days30 = Enumerable.Range(0, 30).Select(i => today.AddDays(-(29 - i))).ToList();
        ViewBag.Trend30Labels = days30.Select(d => d.ToString("MMM d")).ToList();
        ViewBag.Trend30Revenue = days30.Select(d => live.Where(o => o.OrderedAt.Date == d).Sum(o => o.TotalAmount)).ToList();
        ViewBag.Trend30Orders = days30.Select(d => live.Count(o => o.OrderedAt.Date == d)).ToList();

        var statuses = new[] { OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Accepted, OrderStatus.Processing, OrderStatus.ReadyForPickup, OrderStatus.Completed, OrderStatus.Cancelled };
        ViewBag.StatusLabels = statuses.Select(x => x.ToString()).ToList();
        ViewBag.StatusData = statuses.Select(x => orders.Count(o => o.Status == x)).ToList();

        var topProducts = orders.Where(o => o.Status != OrderStatus.Cancelled)
            .SelectMany(o => o.Items)
            .GroupBy(i => string.IsNullOrEmpty(i.ProductNameSnapshot) ? "Product #" + i.ProductId : i.ProductNameSnapshot)
            .Select(g => new { Name = g.Key, Total = g.Sum(i => i.TotalPrice) })
            .OrderByDescending(g => g.Total).Take(5).ToList();
        ViewBag.TopProductLabels = topProducts.Select(t => t.Name).ToList();
        ViewBag.TopProductData = topProducts.Select(t => t.Total).ToList();

        ViewBag.InStockCount = products.Count(p => p.StockQuantityKg >= 5 && p.IsAvailable);
        ViewBag.OutOfStockCount = products.Count(p => p.StockQuantityKg == 0 || !p.IsAvailable);
        ViewBag.LowStockItems = products.Where(p => p.StockQuantityKg < 5).OrderBy(p => p.StockQuantityKg).Take(4).ToList();

        var ratings = await _unitOfWork.Repository<Review>().Query()
            .Where(r => r.FarmerId == FarmerId && r.IsApproved)
            .Select(r => r.Rating).ToListAsync();
        ViewBag.ReviewCount = ratings.Count;
        ViewBag.AvgRating = ratings.Count == 0 ? 0.0 : Math.Round(ratings.Average(), 1);

        ViewBag.ProductCount = products.Count;
        ViewBag.LowStockCount = products.Count(p => p.StockQuantityKg < 5);
        ViewBag.PendingCount = orders.Count(o => o.Status == OrderStatus.Pending);
        ViewBag.ActiveCount = orders.Count(o => o.Status is OrderStatus.Pending or OrderStatus.Accepted or OrderStatus.ReadyForPickup);
        ViewBag.TotalRevenue = orders.Where(o => o.Status == OrderStatus.Completed).Sum(o => o.TotalAmount);
        ViewBag.RecentOrders = orders.Take(5).ToList();
        ViewBag.FarmerStatus = farmer.Status;
        ViewBag.UnreadCount = await UnreadAsync(farmer.UserId);

        var weekStart = DateTime.UtcNow.Date.AddDays(-6);
        var days = Enumerable.Range(0, 7).Select(i => weekStart.AddDays(i)).ToList();
        var earned = orders.Where(o => o.Status == OrderStatus.Completed && o.CompletedAt.HasValue && o.CompletedAt.Value >= weekStart).ToList();
        ViewBag.ChartLabels = days.Select(d => d.ToString("ddd")).ToList();
        ViewBag.ChartRevenue = days.Select(d => earned.Where(o => o.CompletedAt!.Value.Date == d).Sum(o => o.TotalAmount)).ToList();

        var upcoming = await _unitOfWork.Repository<Order>().Query()
            .Include(o => o.Market)
            .Include(o => o.Items)
            .Where(o => o.FarmerId == FarmerId
                        && o.PickupTime != null
                        && o.PickupTime > DateTime.UtcNow
                        && o.Status != OrderStatus.Cancelled
                        && o.Status != OrderStatus.Completed)
            .OrderBy(o => o.PickupTime)
            .Take(5)
            .ToListAsync();
        ViewBag.UpcomingPickups = upcoming;
        return View();
    }

    public IActionResult Settings() => RedirectToAction("Index", "Profile");

    public async Task<IActionResult> Notifications()
    {
        ViewData["Title"] = "Notifications";
        var farmer = await GetFarmerAsync();
        if (farmer is null) return MissingProfileResult(Request.Path);
        var notifications = await _unitOfWork.Repository<Notification>().Query()
            .Where(n => n.UserId == farmer.UserId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();
        return View(notifications);
    }

    [HttpGet]
    public async Task<IActionResult> UnreadCountJson()
    {
        var farmer = await GetFarmerAsync();
        if (farmer is null) return Json(new { count = 0 });
        return Json(new { count = await UnreadAsync(farmer.UserId) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNotificationRead(int id)
    {
        var farmer = await GetFarmerAsync();
        if (farmer is null) return MissingProfileResult(Request.Path);
        var notification = await _unitOfWork.Repository<Notification>().GetByIdAsync(id);
        if (notification != null && notification.UserId == farmer.UserId)
        {
            notification.IsRead = true;
            await _unitOfWork.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Notifications));
    }

    private async Task<MarketLink.Models.Farmer?> GetFarmerAsync()
        => await _unitOfWork.Repository<MarketLink.Models.Farmer>().GetByIdAsync(FarmerId);

    private Task<int> UnreadAsync(string userId)
        => _unitOfWork.Repository<Notification>().CountAsync(n => n.UserId == userId && !n.IsRead);
}
