using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Services
{
    public interface IOrderService
    {
        Task<OrderIndexViewModel> GetOrdersAsync(OrderFilterViewModel filter);
        Task<OrderDetailsViewModel?> GetOrderDetailsAsync(int orderId);
    }
}
