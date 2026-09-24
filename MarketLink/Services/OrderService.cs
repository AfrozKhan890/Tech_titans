using Microsoft.EntityFrameworkCore;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Services
{
    public class OrderService : IOrderService
    {
        private readonly MarketLinkDbContext _context;

        public OrderService(MarketLinkDbContext context)
        {
            _context = context;
        }

        public async Task<OrderIndexViewModel> GetOrdersAsync(OrderFilterViewModel filter)
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Farmer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(o =>
                    (o.Customer != null && o.Customer.FullName.ToLower().Contains(term)) ||
                    (o.Farmer != null && o.Farmer.StallName.ToLower().Contains(term)) ||
                    o.OrderId.ToString() == term);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(o => o.OrderStatus == filter.Status.Value);
            }

            if (filter.FarmerId.HasValue)
            {
                query = query.Where(o => o.FarmerId == filter.FarmerId.Value);
            }

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

            var totalCount = await query.CountAsync();

            var pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;
            var pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;

            var items = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var revenueBase = _context.Orders.Where(o => o.OrderStatus != OrderStatus.Cancelled);

            var vm = new OrderIndexViewModel
            {
                Filter = filter,
                Result = new PagedResult<Order>
                {
                    Items = items,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount
                },
                Farmers = await _context.Farmers.OrderBy(f => f.StallName).ToListAsync(),
                TotalOrders = await _context.Orders.CountAsync(),
                PlacedCount = await _context.Orders.CountAsync(o => o.OrderStatus == OrderStatus.Placed),
                AcceptedCount = await _context.Orders.CountAsync(o => o.OrderStatus == OrderStatus.Accepted),
                ReadyForPickupCount = await _context.Orders.CountAsync(o => o.OrderStatus == OrderStatus.ReadyForPickup),
                CompletedCount = await _context.Orders.CountAsync(o => o.OrderStatus == OrderStatus.Completed),
                CancelledCount = await _context.Orders.CountAsync(o => o.OrderStatus == OrderStatus.Cancelled),
                TotalRevenue = await revenueBase.SumAsync(o => o.TotalAmount)
            };

            return vm;
        }

        public async Task<OrderDetailsViewModel?> GetOrderDetailsAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Farmer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return null;
            }

            return new OrderDetailsViewModel { Order = order };
        }
    }
}
