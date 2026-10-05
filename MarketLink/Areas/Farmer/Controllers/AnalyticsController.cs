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
public class AnalyticsController : MarketLink.Services.FarmerAreaController
{
    private readonly IUnitOfWork _unitOfWork;

    public AnalyticsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Sales & Performance Analytics";
        int farmerId = FarmerId;

        var orders = await _unitOfWork.Repository<Order>().Query()
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Where(o => o.FarmerId == farmerId)
            .ToListAsync();

        var completedOrders = orders.Where(o => o.Status == OrderStatus.Completed).ToList();
        var pendingOrders = orders.Where(o => o.Status == OrderStatus.Pending).ToList();

        ViewBag.TotalRevenue = completedOrders.Sum(o => o.TotalAmount);
        ViewBag.TotalOrders = orders.Count;
        ViewBag.CompletedCount = completedOrders.Count;
        ViewBag.PendingCount = pendingOrders.Count;
        ViewBag.AverageOrderValue = completedOrders.Any() ? completedOrders.Average(o => o.TotalAmount) : 0;


        var topProducts = completedOrders
            .SelectMany(o => o.Items)
            .GroupBy(i => new { i.ProductId, i.ProductNameSnapshot })
            .Select(g => new
            {
                ProductId = g.Key.ProductId,
                Name = g.Key.ProductNameSnapshot,
                TotalKg = g.Sum(x => x.QuantityKg),
                Revenue = g.Sum(x => x.TotalPrice)
            })
            .OrderByDescending(x => x.Revenue)
            .Take(5)
            .ToList();

        ViewBag.TopProducts = topProducts;


        var last7Days = Enumerable.Range(0, 7)
            .Select(i => DateTime.UtcNow.Date.AddDays(-6 + i))
            .ToList();

        var dailyLabels = last7Days.Select(d => d.ToString("ddd (MMM dd)")).ToList();
        var dailyRevenues = last7Days.Select(d => completedOrders
            .Where(o => o.CompletedAt.HasValue && o.CompletedAt.Value.Date == d)
            .Sum(o => o.TotalAmount)).ToList();

        ViewBag.ChartLabels = dailyLabels;
        ViewBag.ChartData = dailyRevenues;

        return View();
    }
}
