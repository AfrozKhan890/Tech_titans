using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Areas.Admin.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync();
    }
}
