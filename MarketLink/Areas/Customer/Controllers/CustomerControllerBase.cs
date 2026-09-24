using MarketLink.Areas.Customer.Services;
using MarketLink.Data;
using CustomerEntity = MarketLink.Models.Customer;
using Microsoft.AspNetCore.Mvc;

namespace MarketLink.Areas.Customer.Controllers
{
    public abstract class CustomerControllerBase : Controller
    {
        protected MarketLinkDbContext Db { get; }
        protected ICustomerIdentityService IdentityService { get; }

        protected CustomerControllerBase(MarketLinkDbContext db, ICustomerIdentityService identityService)
        {
            Db = db;
            IdentityService = identityService;
        }

        protected async Task<CustomerEntity?> CurrentCustomerAsync() =>
            await IdentityService.GetCurrentAsync(User);
    }
}
