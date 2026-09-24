using Microsoft.EntityFrameworkCore;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Areas.Admin.Services
{
    /// <summary>
    /// Computes every Reports figure from the database, honouring the
    /// optional date/market/farmer/status filters. No hardcoded data.
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly MarketLinkDbContext _context;

        public ReportService(MarketLinkDbContext context)
        {
            _context = context;
        }

        public async Task<ReportsIndexViewModel> GetReportAsync(ReportFilterViewModel filter)
        {
            var query = _context.Orders
                .Include(o => o.Farmer)
                    .ThenInclude(f => f!.Market)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Customer)
                .AsQueryable();

            if (filter.DateFrom.HasValue)
            {
                var from = filter.DateFrom.Value.Date;
                query = query.Where(o => o.OrderDate >= from);
            }

            if (filter.DateTo.HasValue)
            {
                var to = filter.DateTo.Value.Date.AddDays(1);
                query = query.Where(o => o.OrderDate < to);
            }

            if (filter.MarketId.HasValue)
            {
                query = query.Where(o => o.Farmer != null && o.Farmer.MarketId == filter.MarketId.Value);
            }

            if (filter.FarmerId.HasValue)
            {
                query = query.Where(o => o.FarmerId == filter.FarmerId.Value);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(o => o.OrderStatus == filter.Status.Value);
            }

            // Materialise the filtered set once. Reports are admin-facing and
            // bounded by the filters, so an in-memory aggregation is simpler
            // and provider-agnostic compared to grouped SQL with conditional
            // sums. Only minimal columns are pulled for the breakdowns below.
            var filteredOrders = await query
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderStatus,
                    o.OrderDate,
                    o.TotalAmount,
                    o.CustomerId,
                    o.Customer!.FullName,
                    o.FarmerId,
                    o.Farmer!.StallName,
                    MarketId = o.Farmer.MarketId,
                    MarketName = o.Farmer.Market != null ? o.Farmer.Market.MarketName : null,
                    Items = o.OrderItems.Select(oi => new { oi.ProductId, oi.Product!.Name, oi.Quantity, oi.LineTotal }).ToList()
                })
                .ToListAsync();

            var nonCancelled = filteredOrders.Where(o => o.OrderStatus != OrderStatus.Cancelled).ToList();

            var vm = new ReportsIndexViewModel
            {
                Filter = filter,
                Markets = await _context.Markets.OrderBy(m => m.MarketName).ToListAsync(),
                Farmers = await _context.Farmers.OrderBy(f => f.StallName).ToListAsync(),

                TotalOrders = filteredOrders.Count,
                CompletedOrders = filteredOrders.Count(o => o.OrderStatus == OrderStatus.Completed),
                CancelledOrders = filteredOrders.Count(o => o.OrderStatus == OrderStatus.Cancelled),
                TotalRevenue = nonCancelled.Sum(o => o.TotalAmount),
                AverageOrderValue = nonCancelled.Any()
                    ? Math.Round(nonCancelled.Sum(o => o.TotalAmount) / nonCancelled.Count, 2)
                    : 0m
            };

            vm.OrderStatusBreakdown = filteredOrders
                .GroupBy(o => o.OrderStatus)
                .Select(g => new OrderStatusCount { Status = g.Key.ToString(), Count = g.Count() })
                .OrderBy(s => s.Status)
                .ToList();

            vm.OrdersByMarket = nonCancelled
                .Where(o => o.MarketId.HasValue)
                .GroupBy(o => new { o.MarketId, o.MarketName })
                .Select(g => new MarketOrderReportRow
                {
                    MarketId = g.Key.MarketId ?? 0,
                    MarketName = g.Key.MarketName ?? "-",
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(r => r.Revenue)
                .ToList();

            vm.OrdersByFarmer = nonCancelled
                .GroupBy(o => new { o.FarmerId, o.StallName })
                .Select(g => new FarmerOrderReportRow
                {
                    FarmerId = g.Key.FarmerId,
                    StallName = g.Key.StallName,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(r => r.Revenue)
                .Take(20)
                .ToList();

            // Revenue by month — span exactly the filtered range if provided,
            // otherwise the last 6 months up to "now".
            var now = DateTime.UtcNow;
            var rangeEnd = filter.DateTo?.Date ?? new DateTime(now.Year, now.Month, 1);
            var rangeStart = filter.DateFrom?.Date ?? rangeEnd.AddMonths(-5);

            // Normalise both ends to the first of their month.
            var firstMonth = new DateTime(rangeStart.Year, rangeStart.Month, 1);
            var lastMonth = new DateTime(rangeEnd.Year, rangeEnd.Month, 1);

            var monthCount = ((lastMonth.Year - firstMonth.Year) * 12) + lastMonth.Month - firstMonth.Month + 1;
            if (monthCount < 1) monthCount = 1;
            if (monthCount > 24) monthCount = 24; // cap for safety on charts

            vm.RevenueByMonth = Enumerable.Range(0, monthCount)
                .Select(offset => firstMonth.AddMonths(offset))
                .Select(monthStart => new MonthlyRevenuePoint
                {
                    MonthLabel = monthStart.ToString("MMM yyyy"),
                    Revenue = nonCancelled
                        .Where(o => o.OrderDate.Year == monthStart.Year && o.OrderDate.Month == monthStart.Month)
                        .Sum(o => o.TotalAmount)
                })
                .ToList();

            vm.TopProducts = nonCancelled
                .SelectMany(o => o.Items)
                .GroupBy(i => i.Name)
                .Select(g => new TopProduct
                {
                    ProductName = g.Key,
                    UnitsSold = g.Sum(i => i.Quantity)
                })
                .OrderByDescending(p => p.UnitsSold)
                .Take(10)
                .ToList();

            vm.TopCustomers = nonCancelled
                .GroupBy(o => new { o.CustomerId, o.FullName })
                .Select(g => new CustomerActivityRow
                {
                    CustomerId = g.Key.CustomerId,
                    FullName = g.Key.FullName,
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(c => c.TotalSpent)
                .Take(10)
                .ToList();

            return vm;
        }
    }
}