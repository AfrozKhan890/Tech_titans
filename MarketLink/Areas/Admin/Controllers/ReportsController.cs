using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MarketLink.Repositories;
using MarketLink.Models;
using MarketLink.Models.Enums;
using FarmerModel = global::MarketLink.Models.Farmer;
using CustomerModel = global::MarketLink.Models.Customer;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace MarketLink.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ReportsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    public ReportsController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        ViewData["Title"] = "Platform Reports & Exports";
        var fromDate = from?.Date;
        var toExclusive = to?.Date.AddDays(1);
        var orders = _unitOfWork.Repository<Order>().Query().AsNoTracking();
        if (fromDate.HasValue) orders = orders.Where(o => o.OrderedAt >= fromDate.Value);
        if (toExclusive.HasValue) orders = orders.Where(o => o.OrderedAt < toExclusive.Value);

        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        ViewBag.TotalOrders = await orders.CountAsync();
        ViewBag.TotalFarmers = await _unitOfWork.Repository<FarmerModel>().CountAsync();
        ViewBag.TotalCustomers = await _unitOfWork.Repository<CustomerModel>().CountAsync();
        ViewBag.TotalProducts = await _unitOfWork.Repository<Product>().CountAsync();
        ViewBag.TotalRevenue = await orders.Where(o => o.Status == OrderStatus.Completed).Select(o => (decimal?)o.TotalAmount).SumAsync() ?? 0m;

        var marketRevenue = await orders.Where(o => o.Status == OrderStatus.Completed && o.PickupSlot != null && o.PickupSlot.Market != null)
            .GroupBy(o => new { o.PickupSlot!.MarketId, Name = o.PickupSlot.Market!.Name })
            .Select(g => new { g.Key.MarketId, g.Key.Name, Revenue = g.Sum(o => o.TotalAmount), Orders = g.Count() })
            .OrderByDescending(x => x.Revenue).Take(20).ToListAsync();

        var farmerActivity = await orders.Where(o => o.Status != OrderStatus.Cancelled)
            .GroupBy(o => new { o.FarmerId, Name = o.Farmer.FarmName })
            .Select(g => new { g.Key.FarmerId, g.Key.Name, Orders = g.Count(), CompletedOrders = g.Count(o => o.Status == OrderStatus.Completed), Revenue = g.Where(o => o.Status == OrderStatus.Completed).Sum(o => o.TotalAmount) })
            .OrderByDescending(x => x.Orders).ThenByDescending(x => x.Revenue).Take(20).ToListAsync();

        var productStats = await _unitOfWork.Repository<OrderItem>().Query().AsNoTracking()
            .Where(i => i.Order.Status == OrderStatus.Completed &&
                        (!fromDate.HasValue || i.Order.OrderedAt >= fromDate.Value) &&
                        (!toExclusive.HasValue || i.Order.OrderedAt < toExclusive.Value))
            .GroupBy(i => new { i.ProductId, Name = i.Product.Name, Farmer = i.Product.Farmer.FarmName })
            .Select(g => new { g.Key.ProductId, g.Key.Name, g.Key.Farmer, Quantity = g.Sum(i => i.QuantityKg), Revenue = g.Sum(i => i.TotalPrice), Orders = g.Select(i => i.OrderId).Distinct().Count() })
            .OrderByDescending(x => x.Quantity).ThenByDescending(x => x.Revenue).Take(20).ToListAsync();

        ViewBag.MarketRevenue = marketRevenue;
        ViewBag.FarmerActivity = farmerActivity;
        ViewBag.ProductStats = productStats;
        return View();
    }

    public async Task<IActionResult> Analytics(DateTime? from, DateTime? to)
    {
        var fromDate = (from ?? DateTime.UtcNow.Date.AddDays(-6)).Date;
        var toDate = (to ?? DateTime.UtcNow.Date).Date;
        if (toDate < fromDate) toDate = fromDate;
        var toExclusive = toDate.AddDays(1);
        var orders = _unitOfWork.Repository<Order>().Query().AsNoTracking()
            .Where(o => o.OrderedAt >= fromDate && o.OrderedAt < toExclusive);
        var days = Enumerable.Range(0, (toDate - fromDate).Days + 1).Select(i => fromDate.AddDays(i)).ToList();
        var daily = await orders.GroupBy(o => o.OrderedAt.Date).Select(g => new { Day = g.Key, Orders = g.Count(), Revenue = g.Where(o => o.Status == OrderStatus.Completed).Sum(o => o.TotalAmount) }).ToListAsync();
        ViewBag.DaysLabels = days.Select(d => d.ToString("ddd (MMM dd)")).ToList();
        ViewBag.OrdersPerDay = days.Select(d => daily.FirstOrDefault(x => x.Day == d)?.Orders ?? 0).ToList();
        ViewBag.RevenuePerDay = days.Select(d => daily.FirstOrDefault(x => x.Day == d)?.Revenue ?? 0m).ToList();
        ViewBag.TotalOrders = await orders.CountAsync();
        ViewBag.TotalFarmers = await _unitOfWork.Repository<FarmerModel>().CountAsync();
        ViewBag.TotalCustomers = await _unitOfWork.Repository<CustomerModel>().CountAsync();
        ViewBag.From = fromDate.ToString("yyyy-MM-dd");
        ViewBag.To = toDate.ToString("yyyy-MM-dd");
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ExportOrdersCsv(DateTime? from, DateTime? to)
    {
        IQueryable<Order> query = _unitOfWork.Repository<Order>().Query().AsNoTracking()
            .Include(o => o.Farmer).Include(o => o.PickupSlot).ThenInclude(ps => ps!.Market).Include(o => o.Items);
        if (from.HasValue) query = query.Where(o => o.OrderedAt >= from.Value.Date);
        if (to.HasValue) query = query.Where(o => o.OrderedAt < to.Value.Date.AddDays(1));
        var orders = await query.OrderByDescending(o => o.OrderedAt).ToListAsync();
        var sb = new StringBuilder("OrderNumber,Date,Farmer,Market,ItemsCount,TotalAmount,Status\n");
        foreach (var o in orders) sb.AppendLine(string.Join(",", Csv(o.OrderNumber), Csv(o.OrderedAt.ToString("yyyy-MM-dd HH:mm")), Csv(o.Farmer?.FarmName), Csv(o.PickupSlot?.Market?.Name), o.Items.Count, o.TotalAmount.ToString("F2"), Csv(o.Status.ToString())));
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"MarketLink_Orders_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportFarmersCsv()
    {
        var farmers = await _unitOfWork.Repository<FarmerModel>().Query().AsNoTracking().Include(f => f.Products).OrderBy(f => f.FarmName).ToListAsync();
        var sb = new StringBuilder("Id,FarmName,City,State,ProductsCount,Status,RegisteredDate\n");
        foreach (var f in farmers) sb.AppendLine(string.Join(",", f.Id, Csv(f.FarmName), Csv(f.City), Csv(f.State), f.Products.Count, Csv(f.Status.ToString()), f.RegisteredAt.ToString("yyyy-MM-dd")));
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"MarketLink_Farmers_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    private static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
}
