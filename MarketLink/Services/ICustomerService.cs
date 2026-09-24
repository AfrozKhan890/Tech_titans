using MarketLink.Models;
using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Services
{
    public interface ICustomerService
    {
        Task<CustomerIndexViewModel> GetCustomersAsync(CustomerFilterViewModel filter);
        Task<CustomerDetailsViewModel?> GetCustomerDetailsAsync(int customerId);
        Task<bool> ChangeStatusAsync(int customerId, CustomerStatus newStatus);
    }
}
