using Microsoft.EntityFrameworkCore;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Areas.Admin.Services
{
    /// <summary>
    /// Produces the trend data used by the Analytics charts. Separate from
    /// ReportService so the two pages can evolve independently, but both
    /// source data exclusively from the database.
    /// </summary>
    public class AnalyticsService : IAnalyticsService
    {
        private readonly MarketLinkDbContext _context;

        public AnalyticsService(MarketLinkDbContext context)
        {
            _context = context;
        }

        public async Task<AnalyticsViewModel> GetAnalyticsAsync(AnalyticsFilterViewModel filter)
        {
            // Clamp months to a sane range so the chart doesn't explode.
            var months = filter.Months;
            if (months != 3 && months != 6 && months != 12)
            {
                months = 6;
            }

            var now = DateTime.UtcNow;
            var firstOfThisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var windowStart = firstOfThisMonth.AddMonths(-(months - 1));

            var vm = new AnalyticsViewModel
            {
                Filter = new AnalyticsFilterViewModel { Months = months },

                TotalOrders = await _context.Orders.CountAsync(),
                TotalFarmers = await _context.Farmers.CountAsync(),
                TotalCustomers = await _context.Customers.CountAsync()
            };

            vm.TotalRevenue = await _context.Orders
                .Where(o => o.OrderStatus != OrderStatus.Cancelled)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            // Orders within the window (non-cancelled for revenue, all for count).
            var windowOrders = await _context.Orders
                .Where(o => o.OrderDate >= windowStart)
                .Select(o => new { o.OrderId, o.OrderDate, o.TotalAmount, o.OrderStatus })
                .ToListAsync();

            var monthStarts = Enumerable.Range(0, months)
                .Select(offset => windowStart.AddMonths(offset))
                .ToList();

            vm.RevenueTrend = monthStarts
                .Select(monthStart => new MonthlyRevenuePoint
                {
                    MonthLabel = monthStart.ToString("MMM yyyy"),
                    Revenue = windowOrders
                        .Where(o => o.OrderStatus != OrderStatus.Cancelled
                                    && o.OrderDate.Year == monthStart.Year
                                    && o.OrderDate.Month == monthStart.Month)
                        .Sum(o => o.TotalAmount)
                })
                .ToList();

            vm.OrderTrend = monthStarts
                .Select(monthStart => new MonthlyOrderPoint
                {
                    MonthLabel = monthStart.ToString("MMM yyyy"),
                    OrderCount = windowOrders
                        .Count(o => o.OrderDate.Year == monthStart.Year
                                    && o.OrderDate.Month == monthStart.Month)
                })
                .ToList();

            vm.OrderStatusBreakdown = await _context.Orders
                .GroupBy(o => o.OrderStatus)
                .Select(g => new OrderStatusCount { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            // Orders by market (non-cancelled, all-time).
            var marketAggregates = await _context.Orders
                .Where(o => o.OrderStatus != OrderStatus.Cancelled && o.Farmer != null && o.Farmer.MarketId != null)
                .GroupBy(o => new { MarketId = o.Farmer!.MarketId, MarketName = o.Farmer!.Market!.MarketName })
                .Select(g => new MarketOrderReportRow
                {
                    MarketId = g.Key.MarketId ?? 0,
                    MarketName = g.Key.MarketName ?? "-",
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(r => r.Revenue)
                .ToListAsync();
            vm.OrdersByMarket = marketAggregates;

            vm.TopProducts = await _context.OrderItems
                .Where(oi => oi.Order!.OrderStatus != OrderStatus.Cancelled)
                .GroupBy(oi => oi.Product!.Name)
                .Select(g => new TopProduct
                {
                    ProductName = g.Key,
                    UnitsSold = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(p => p.UnitsSold)
                .Take(8)
                .ToListAsync();

            vm.TopFarmers = await _context.Farmers
                .Select(f => new TopFarmer
                {
                    StallName = f.StallName,
                    OrderCount = f.Orders.Count(o => o.OrderStatus != OrderStatus.Cancelled)
                })
                .OrderByDescending(f => f.OrderCount)
                .Take(8)
                .ToListAsync();

            return vm;
        }
    }
}