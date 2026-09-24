using System.Security.Claims;
using MarketLink.Data;
using CustomerEntity = MarketLink.Models.Customer;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Areas.Customer.Services
{
    public interface ICustomerIdentityService
    {
        Task<CustomerEntity?> GetCurrentAsync(ClaimsPrincipal principal);
    }

    public sealed class CustomerIdentityService : ICustomerIdentityService
    {
        private readonly MarketLinkDbContext _context;
        public CustomerIdentityService(MarketLinkDbContext context) => _context = context;

        public async Task<CustomerEntity?> GetCurrentAsync(ClaimsPrincipal principal)
        {
            if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
                return null;
            return await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
        }
    }
}
