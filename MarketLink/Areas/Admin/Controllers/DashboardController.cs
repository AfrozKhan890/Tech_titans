using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using MarketLink.Repositories;
using MarketLink.Services;
using MarketLink.Models;
using MarketLink.Models.Enums;
using Microsoft.EntityFrameworkCore;
using FarmerEntity = MarketLink.Models.Farmer;
using CustomerEntity = MarketLink.Models.Customer;

namespace MarketLink.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        UserManager<ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Admin Overview";

        var farmers = await _unitOfWork.Repository<FarmerEntity>().Query().Include(f => f.User).ToListAsync();
        var customers = await _unitOfWork.Repository<CustomerEntity>().Query().Include(c => c.User).ToListAsync();
        var products = await _unitOfWork.Repository<Product>().Query()
            .Include(p => p.Category).ThenInclude(c => c!.Parent)
            .Include(p => p.Farmer)
            .ToListAsync();
        var markets = await _unitOfWork.Repository<Market>().GetAllAsync();
        var orders = await _unitOfWork.Repository<Order>().Query()
            .Include(o => o.Customer).ThenInclude(c => c.User)
            .Include(o => o.Farmer)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var live = orders.Where(o => o.Status != OrderStatus.Cancelled).ToList();

        decimal Sum(DateTime fromInclusive, DateTime toExclusive) =>
            live.Where(o => o.OrderedAt >= fromInclusive && o.OrderedAt < toExclusive).Sum(o => o.TotalAmount);
        double Trend(decimal current, decimal previous) =>
            previous == 0 ? (current > 0 ? 100 : 0) : (double)((current - previous) / previous * 100m);

        // ----- KPI cards -----
        var completed = orders.Where(o => o.Status == OrderStatus.Completed).ToList();
        ViewBag.CustomerCount = customers.Count;
        ViewBag.FarmerCount = farmers.Count(f => f.Status == FarmerStatus.Approved);
        ViewBag.PendingFarmers = farmers.Count(f => f.Status == FarmerStatus.Pending);
        ViewBag.ProductCount = products.Count;
        ViewBag.MarketCount = markets.Count();
        ViewBag.OrderCount = orders.Count;
        ViewBag.TotalRevenue = completed.Sum(o => o.TotalAmount);
        ViewBag.AvgOrderValue = live.Count == 0 ? 0m : live.Average(o => o.TotalAmount);
        ViewBag.CompletionRate = orders.Count == 0 ? 0.0 : Math.Round(completed.Count * 100.0 / orders.Count, 1);
        ViewBag.PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending);

        var weekStart = today.AddDays(-6);
        var prevWeekStart = today.AddDays(-13);
        var thisWeekRevenue = Sum(weekStart, today.AddDays(1));
        var prevWeekRevenue = Sum(prevWeekStart, weekStart);
        ViewBag.RevenueTrend = Math.Round(Trend(thisWeekRevenue, prevWeekRevenue), 1);
        ViewBag.WeekRevenue = thisWeekRevenue;

        var thisWeekOrders = live.Count(o => o.OrderedAt >= weekStart);
        var prevWeekOrders = live.Count(o => o.OrderedAt >= prevWeekStart && o.OrderedAt < weekStart);
        ViewBag.OrdersTrend = Math.Round(Trend(thisWeekOrders, prevWeekOrders), 1);

        var newCustomers = customers.Count(c => c.User != null && c.User.CreatedAt >= weekStart);
        var prevCustomers = customers.Count(c => c.User != null && c.User.CreatedAt >= prevWeekStart && c.User.CreatedAt < weekStart);
        ViewBag.CustomerTrend = Math.Round(Trend(newCustomers, prevCustomers), 1);
        ViewBag.NewCustomers = newCustomers;

        // ----- Time series (30 days, the view slices the last 7 for the toggle) -----
        var days = Enumerable.Range(0, 30).Select(i => today.AddDays(-(29 - i))).ToList();
        ViewBag.Trend30Labels = days.Select(d => d.ToString("MMM d")).ToList();
        ViewBag.Trend30Revenue = days.Select(d => live.Where(o => o.OrderedAt.Date == d).Sum(o => o.TotalAmount)).ToList();
        ViewBag.Trend30Orders = days.Select(d => live.Count(o => o.OrderedAt.Date == d)).ToList();

        // ----- Orders by status -----
        var statusOrder = new[] { OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Accepted, OrderStatus.Processing, OrderStatus.ReadyForPickup, OrderStatus.Completed, OrderStatus.Cancelled };
        ViewBag.StatusLabels = statusOrder.Select(x => x.ToString()).ToList();
        ViewBag.StatusData = statusOrder.Select(x => orders.Count(o => o.Status == x)).ToList();

        // ----- Orders by weekday -----
        var weekdays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
        ViewBag.WeekdayLabels = weekdays.Select(d => d.ToString()[..3]).ToList();
        ViewBag.WeekdayData = weekdays.Select(d => live.Count(o => o.OrderedAt.DayOfWeek == d)).ToList();

        // ----- Top farmers by order value -----
        var topFarmers = live.GroupBy(o => o.Farmer?.FarmName ?? "Unknown")
            .Select(g => new { Name = g.Key, Total = g.Sum(o => o.TotalAmount) })
            .OrderByDescending(g => g.Total).Take(5).ToList();
        ViewBag.TopFarmerLabels = topFarmers.Select(f => f.Name).ToList();
        ViewBag.TopFarmerData = topFarmers.Select(f => f.Total).ToList();

        // ----- Products by top-level category -----
        var categoryBreakdown = products.Where(p => p.Category != null)
            .GroupBy(p => p.Category!.Parent?.Name ?? p.Category!.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count).Take(6).ToList();
        ViewBag.CategoryLabels = categoryBreakdown.Select(c => c.Name).ToList();
        ViewBag.CategoryData = categoryBreakdown.Select(c => c.Count).ToList();

        // ----- Customer sign-ups, last 8 weeks -----
        var weeks = Enumerable.Range(0, 8).Select(i => today.AddDays(-7 * (7 - i))).ToList();
        ViewBag.GrowthLabels = weeks.Select(w => "Wk of " + w.ToString("MMM d")).ToList();
        ViewBag.GrowthData = weeks.Select(w => customers.Count(c => c.User != null && c.User.CreatedAt.Date >= w && c.User.CreatedAt.Date < w.AddDays(7))).ToList();

        // ----- Lists -----
        ViewBag.RecentOrders = orders.OrderByDescending(o => o.OrderedAt).Take(6).ToList();
        ViewBag.PendingFarmerList = farmers.Where(f => f.Status == FarmerStatus.Pending).OrderBy(f => f.RegisteredAt).Take(5).ToList();
        ViewBag.LowStock = products.Where(p => p.StockQuantityKg < 10).OrderBy(p => p.StockQuantityKg).Take(5).ToList();

        return View();
    }

    public async Task<IActionResult> Announcements()
    {
        ViewData["Title"] = "Announcement Management";
        var items = await _unitOfWork.Repository<Announcement>().Query().OrderByDescending(a => a.CreatedAt).ToListAsync();
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAnnouncement(string title, string message, bool isActive = true, DateTime? startsAt = null, DateTime? endsAt = null)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message) || (startsAt.HasValue && endsAt.HasValue && endsAt < startsAt))
        { TempData["Error"] = "Valid title/message and date range are required."; return RedirectToAction(nameof(Announcements)); }
        await _unitOfWork.Repository<Announcement>().AddAsync(new Announcement { Title = title.Trim(), Message = message.Trim(), IsActive = isActive, StartsAt = startsAt, EndsAt = endsAt });
        await _unitOfWork.SaveChangesAsync(); TempData["Success"] = "Announcement created.";
        return RedirectToAction(nameof(Announcements));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAnnouncement(int id, string title, string message, bool isActive, DateTime? startsAt = null, DateTime? endsAt = null)
    {
        var item = await _unitOfWork.Repository<Announcement>().GetByIdAsync(id);
        if (item == null) return NotFound();
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message) || (startsAt.HasValue && endsAt.HasValue && endsAt < startsAt))
        { TempData["Error"] = "Valid title/message and date range are required."; return RedirectToAction(nameof(Announcements)); }
        item.Title = title.Trim(); item.Message = message.Trim(); item.IsActive = isActive; item.StartsAt = startsAt; item.EndsAt = endsAt; item.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(); TempData["Success"] = "Announcement updated.";
        return RedirectToAction(nameof(Announcements));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAnnouncement(int id)
    {
        var item = await _unitOfWork.Repository<Announcement>().GetByIdAsync(id);
        if (item != null) { _unitOfWork.Repository<Announcement>().Remove(item); await _unitOfWork.SaveChangesAsync(); TempData["Success"] = "Announcement deleted."; }
        return RedirectToAction(nameof(Announcements));
    }

    public async Task<IActionResult> Notifications()
    {
        ViewData["Title"] = "Notification Broadcast Center";
        var notifications = await _unitOfWork.Repository<Notification>().Query()
            .Include(n => n.User)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();

        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Broadcast(string targetRole, string title, string message)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "Title and message are required.";
            return RedirectToAction(nameof(Notifications));
        }


        var allUsers = _userManager.Users.Where(u => u.IsActive).ToList();

        IEnumerable<ApplicationUser> targets = allUsers;
        if (!string.IsNullOrEmpty(targetRole) && targetRole != "All")
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(targetRole);
            targets = usersInRole.Where(u => u.IsActive);
        }

        int sentCount = 0;
        foreach (var u in targets)
        {
            await _notificationService.SendAsync(
                u.Id,
                NotificationType.General,
                title.Trim(),
                message.Trim(),
                "/Home/Index");
            sentCount++;
        }

        TempData["Success"] = $"Broadcast delivered to {sentCount} active users!";
        return RedirectToAction(nameof(Notifications));
    }

    public async Task<IActionResult> Settings()
    {
        ViewData["Title"] = "System & Marketplace Settings";
        var settings = await _unitOfWork.Repository<SiteSetting>().GetAllAsync();
        return View(settings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSetting(string key, string value, string? description)
    {
        var setting = await _unitOfWork.Repository<SiteSetting>()
            .FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
        {
            setting = new SiteSetting
            {
                Key = key,
                Value = value,
                Description = description,
                Group = "General"
            };
            await _unitOfWork.Repository<SiteSetting>().AddAsync(setting);
        }
        else
        {
            setting.Value = value;
            if (!string.IsNullOrEmpty(description)) setting.Description = description;
            _unitOfWork.Repository<SiteSetting>().Update(setting);
        }

        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = $"Setting '{key}' saved successfully!";
        return RedirectToAction(nameof(Settings));
    }
}
