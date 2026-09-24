using Microsoft.EntityFrameworkCore;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly MarketLinkDbContext _context;

        public CustomerService(MarketLinkDbContext context)
        {
            _context = context;
        }

        public async Task<CustomerIndexViewModel> GetCustomersAsync(CustomerFilterViewModel filter)
        {
            var query = _context.Customers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(c =>
                    c.FullName.ToLower().Contains(term) ||
                    c.Email.ToLower().Contains(term) ||
                    (c.Phone != null && c.Phone.Contains(term)));
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(c => c.Status == filter.Status.Value);
            }

            var totalCount = await query.CountAsync();

            var pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;
            var pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;

            var items = await query
                .OrderByDescending(c => c.RegisteredAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var vm = new CustomerIndexViewModel
            {
                Filter = filter,
                Result = new PagedResult<Customer>
                {
                    Items = items,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount
                },
                TotalCustomers = await _context.Customers.CountAsync(),
                ActiveCount = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Active),
                InactiveCount = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Inactive),
                NewThisMonthCount = await _context.Customers.CountAsync(c => c.RegisteredAt >= monthStart)
            };

            return vm;
        }

        public async Task<CustomerDetailsViewModel?> GetCustomerDetailsAsync(int customerId)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerId);
            if (customer == null)
            {
                return null;
            }

            var orders = await _context.Orders
                .Include(o => o.Farmer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderDate)
                .Take(20)
                .ToListAsync();

            var reviews = await _context.Reviews
                .Include(r => r.Product)
                .Include(r => r.Farmer)
                .Where(r => r.CustomerId == customerId && !r.IsRemoved)
                .OrderByDescending(r => r.ReviewDate)
                .Take(10)
                .ToListAsync();

            var allOrders = await _context.Orders
                .Where(o => o.CustomerId == customerId)
                .ToListAsync();

            return new CustomerDetailsViewModel
            {
                Customer = customer,
                Orders = orders,
                Reviews = reviews,
                TotalOrders = allOrders.Count,
                TotalSpent = allOrders.Where(o => o.OrderStatus != OrderStatus.Cancelled).Sum(o => o.TotalAmount),
                CompletedOrders = allOrders.Count(o => o.OrderStatus == OrderStatus.Completed),
                CancelledOrders = allOrders.Count(o => o.OrderStatus == OrderStatus.Cancelled)
            };
        }

        public async Task<bool> ChangeStatusAsync(int customerId, CustomerStatus newStatus)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
            {
                return false;
            }

            customer.Status = newStatus;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
