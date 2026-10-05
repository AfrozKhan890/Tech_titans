using System.Globalization;
using MarketLink.Models;
using MarketLink.Repositories;
using MarketLink.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Areas.Farmer.Controllers;

[Area("Farmer")]
[Authorize(Roles = "Farmer")]
public class InventoryController : MarketLink.Services.FarmerAreaController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public InventoryController(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Weekly Inventory Management";
        int farmerId = FarmerId;
        var (week, year) = CurrentWeek();

        var products = await _unitOfWork.Repository<Product>().Query()
            .Include(p => p.Category)
            .Where(p => p.FarmerId == farmerId)
            .OrderBy(p => p.StockQuantityKg)
            .ToListAsync();

        // Duplicates can exist per product, so keep only the newest row per product (avoids duplicate-key crash)
        var planRows = await _unitOfWork.Repository<Inventory>().Query()
            .Where(i => i.FarmerId == farmerId && i.WeekNumber == week && i.Year == year && !i.IsRecurringTemplate)
            .ToListAsync();
        var plans = planRows
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(i => i.Id).First());
        var templateRows = await _unitOfWork.Repository<Inventory>().Query()
            .Where(i => i.FarmerId == farmerId && i.IsRecurringTemplate)
            .ToListAsync();
        var templates = templateRows
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(i => i.Id).First());

        ViewBag.LowStockCount = products.Count(p => p.StockQuantityKg < 10);
        ViewBag.OutOfStockCount = products.Count(p => p.StockQuantityKg == 0 || !p.IsAvailable);
        ViewBag.Week = week;
        ViewBag.Year = year;
        ViewBag.WeeklyPlans = plans;
        ViewBag.RecurringTemplates = templates;

        return View(products);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveWeeklyPlan(int productId, int availableQuantityKg, decimal pricePerKg)
    {
        var product = await _unitOfWork.Repository<Product>().Query()
            .FirstOrDefaultAsync(p => p.Id == productId && p.FarmerId == FarmerId);
        if (product == null) return NotFound();
        if (availableQuantityKg < 0) { TempData["Error"] = "Weekly quantity cannot be negative."; return RedirectToAction(nameof(Index)); }
        if (pricePerKg < 0) { TempData["Error"] = "Price cannot be negative."; return RedirectToAction(nameof(Index)); }

        var (week, year) = CurrentWeek();
        var inventory = await _unitOfWork.Repository<Inventory>().Query()
            .FirstOrDefaultAsync(i => i.ProductId == productId && i.FarmerId == FarmerId && i.WeekNumber == week && i.Year == year);

        if (inventory == null)
        {
            inventory = new Inventory
            {
                ProductId = productId,
                FarmerId = FarmerId,
                WeekNumber = week,
                Year = year
            };
            await _unitOfWork.Repository<Inventory>().AddAsync(inventory);
        }

        inventory.AvailableQuantityKg = availableQuantityKg;
        inventory.PricePerKg = pricePerKg;
        inventory.UpdatedAt = DateTime.UtcNow;

        var wasOut = product.StockQuantityKg <= 0 || !product.IsAvailable;
        product.StockQuantityKg = availableQuantityKg;
        product.PricePerKg = pricePerKg;
        product.IsAvailable = availableQuantityKg > 0;
        product.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
        if (wasOut && availableQuantityKg > 0)
            await NotifyRestockedProductAsync(productId, product.Name);

        TempData["Success"] = $"Weekly plan saved for {product.Name}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRecurringTemplate(int productId, int availableQuantityKg, decimal pricePerKg)
    {
        var product = await _unitOfWork.Repository<Product>().Query()
            .FirstOrDefaultAsync(p => p.Id == productId && p.FarmerId == FarmerId);
        if (product == null) return NotFound();
        if (availableQuantityKg < 0 || pricePerKg < 0)
        {
            TempData["Error"] = "Template quantity and price cannot be negative.";
            return RedirectToAction(nameof(Index));
        }

        var template = await _unitOfWork.Repository<Inventory>().Query()
            .FirstOrDefaultAsync(i => i.FarmerId == FarmerId && i.ProductId == productId && i.IsRecurringTemplate);
        if (template == null)
        {
            template = new Inventory
            {
                FarmerId = FarmerId,
                ProductId = productId,
                WeekNumber = 0,
                Year = 0,
                IsRecurringTemplate = true
            };
            await _unitOfWork.Repository<Inventory>().AddAsync(template);
        }
        template.AvailableQuantityKg = availableQuantityKg;
        template.PricePerKg = pricePerKg;
        template.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = $"Recurring weekly template saved for {product.Name}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyRecurringTemplates()
    {
        var (week, year) = CurrentWeek();
        var templates = await _unitOfWork.Repository<Inventory>().Query()
            .Where(i => i.FarmerId == FarmerId && i.IsRecurringTemplate)
            .ToListAsync();

        foreach (var source in templates)
        {
            var target = await _unitOfWork.Repository<Inventory>().Query()
                .FirstOrDefaultAsync(i => i.FarmerId == FarmerId && i.ProductId == source.ProductId && i.WeekNumber == week && i.Year == year && !i.IsRecurringTemplate);
            if (target == null)
            {
                target = new Inventory { FarmerId = FarmerId, ProductId = source.ProductId, WeekNumber = week, Year = year };
                await _unitOfWork.Repository<Inventory>().AddAsync(target);
            }
            target.AvailableQuantityKg = source.AvailableQuantityKg;
            target.PricePerKg = source.PricePerKg;
            target.UpdatedAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = "Recurring weekly templates applied to the current week.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyCurrentWeekToNext()
    {
        var (currentWeek, currentYear) = CurrentWeek();
        var nextDate = DateTime.UtcNow.Date.AddDays(7);
        var (nextWeek, nextYear) = WeekOf(nextDate);

        var plans = await _unitOfWork.Repository<Inventory>().Query()
            .Where(i => i.FarmerId == FarmerId && i.WeekNumber == currentWeek && i.Year == currentYear)
            .ToListAsync();

        foreach (var source in plans)
        {
            var target = await _unitOfWork.Repository<Inventory>().Query()
                .FirstOrDefaultAsync(i => i.FarmerId == FarmerId && i.ProductId == source.ProductId && i.WeekNumber == nextWeek && i.Year == nextYear);
            if (target == null)
            {
                target = new Inventory
                {
                    FarmerId = FarmerId,
                    ProductId = source.ProductId,
                    WeekNumber = nextWeek,
                    Year = nextYear
                };
                await _unitOfWork.Repository<Inventory>().AddAsync(target);
            }
            target.AvailableQuantityKg = source.AvailableQuantityKg;
            target.PricePerKg = source.PricePerKg;
            target.UpdatedAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = $"Current weekly plan copied to week {nextWeek} of {nextYear}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStock(int id, int stockQuantityKg, bool isAvailable)
    {
        var product = await _unitOfWork.Repository<Product>().Query()
            .FirstOrDefaultAsync(p => p.Id == id && p.FarmerId == FarmerId);
        if (product == null) return NotFound();
        if (stockQuantityKg < 0) { TempData["Error"] = "Stock cannot be negative."; return RedirectToAction(nameof(Index)); }

        var wasOut = product.StockQuantityKg <= 0 || !product.IsAvailable;
        product.StockQuantityKg = stockQuantityKg;
        product.IsAvailable = isAvailable && stockQuantityKg > 0;
        product.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        if (wasOut && product.StockQuantityKg > 0 && product.IsAvailable)
            await NotifyRestockedProductAsync(product.Id, product.Name);

        TempData["Success"] = $"Stock updated for {product.Name}!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickRestock(int id, int addQuantity)
    {
        var product = await _unitOfWork.Repository<Product>().Query()
            .FirstOrDefaultAsync(p => p.Id == id && p.FarmerId == FarmerId);
        if (product == null) return NotFound();
        if (addQuantity <= 0) { TempData["Error"] = "Restock quantity must be greater than zero."; return RedirectToAction(nameof(Index)); }

        var wasOut = product.StockQuantityKg <= 0 || !product.IsAvailable;
        product.StockQuantityKg += addQuantity;
        product.IsAvailable = true;
        product.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        if (wasOut)
            await NotifyRestockedProductAsync(product.Id, product.Name);

        TempData["Success"] = $"Added +{addQuantity} {product.Unit} to {product.Name}!";
        return RedirectToAction(nameof(Index));
    }

    private async Task NotifyRestockedProductAsync(int productId, string productName)
    {
        var favorites = await _unitOfWork.Repository<Favorite>().Query()
            .Include(f => f.Customer).ThenInclude(c => c.User)
            .Where(f => f.ProductId == productId)
            .ToListAsync();

        foreach (var favorite in favorites)
        {
            await _notificationService.SendAsync(
                favorite.Customer.UserId,
                MarketLink.Models.Enums.NotificationType.LowStock,
                "Favourite product restocked",
                $"{productName} is available again.",
                $"/Products/Details/{productId}");
        }
    }

    private static (int Week, int Year) CurrentWeek() => WeekOf(DateTime.UtcNow.Date);

    private static (int Week, int Year) WeekOf(DateTime date)
    {
        var week = ISOWeek.GetWeekOfYear(date);
        var year = ISOWeek.GetYear(date);
        return (week, year);
    }
}
