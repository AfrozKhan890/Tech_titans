using Microsoft.EntityFrameworkCore;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Areas.Admin.Services
{
    /// <summary>
    /// Computes every Dashboard statistic directly from the database.
    /// No hard-coded/fake numbers are used, per project requirements.
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly MarketLinkDbContext _context;

        public DashboardService(MarketLinkDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            var vm = new DashboardViewModel
            {
                TotalFarmers = await _context.Farmers.CountAsync(),
                TotalCustomers = await _context.Customers.CountAsync(),
                TotalMarkets = await _context.Markets.CountAsync(),
                TotalOrders = await _context.Orders.CountAsync(),
                PendingFarmerApprovals = await _context.Farmers.CountAsync(f => f.Status == FarmerStatus.Pending)
            };

            var now = DateTime.UtcNow;
            var startOfThisMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfThisMonth.AddMonths(-1);

            vm.RevenueThisMonth = await _context.Orders
                .Where(o => o.OrderStatus != OrderStatus.Cancelled && o.OrderDate >= startOfThisMonth)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            vm.RevenueLastMonth = await _context.Orders
                .Where(o => o.OrderStatus != OrderStatus.Cancelled && o.OrderDate >= startOfLastMonth && o.OrderDate < startOfThisMonth)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            vm.RecentOrders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Farmer)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            vm.RecentReviews = await _context.Reviews
                .Include(r => r.Customer)
                .Include(r => r.Farmer)
                .Include(r => r.Product)
                .Where(r => !r.IsRemoved)
                .OrderByDescending(r => r.ReviewDate)
                .Take(5)
                .ToListAsync();

            vm.PendingFarmers = await _context.Farmers
                .Where(f => f.Status == FarmerStatus.Pending)
                .OrderByDescending(f => f.RegisteredAt)
                .Take(5)
                .ToListAsync();

            vm.OrderStatusBreakdown = await _context.Orders
                .GroupBy(o => o.OrderStatus)
                .Select(g => new OrderStatusCount { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            // Revenue trend for the last 6 months (computed in-memory after pulling
            // the minimal columns needed, since SQL Server grouping by
            // year/month works differently across providers).
            var sixMonthsAgo = startOfThisMonth.AddMonths(-5);
            var recentOrders = await _context.Orders
                .Where(o => o.OrderStatus != OrderStatus.Cancelled && o.OrderDate >= sixMonthsAgo)
                .Select(o => new { o.OrderDate, o.TotalAmount })
                .ToListAsync();

            vm.RevenueTrend = Enumerable.Range(0, 6)
                .Select(offset => startOfThisMonth.AddMonths(-5 + offset))
                .Select(monthStart => new MonthlyRevenuePoint
                {
                    MonthLabel = monthStart.ToString("MMM yyyy"),
                    Revenue = recentOrders
                        .Where(o => o.OrderDate.Year == monthStart.Year && o.OrderDate.Month == monthStart.Month)
                        .Sum(o => o.TotalAmount)
                })
                .ToList();

            vm.MostActiveFarmers = await _context.Farmers
                .Select(f => new TopFarmer
                {
                    StallName = f.StallName,
                    OrderCount = f.Orders.Count
                })
                .OrderByDescending(f => f.OrderCount)
                .Take(5)
                .ToListAsync();

            vm.PopularProducts = await _context.OrderItems
                .GroupBy(oi => oi.Product!.Name)
                .Select(g => new TopProduct
                {
                    ProductName = g.Key,
                    UnitsSold = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(p => p.UnitsSold)
                .Take(5)
                .ToListAsync();

            return vm;
        }
    }
}
