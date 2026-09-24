using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Areas.Admin.Services
{
    public interface IAnalyticsService
    {
        Task<AnalyticsViewModel> GetAnalyticsAsync(AnalyticsFilterViewModel filter);
    }
}