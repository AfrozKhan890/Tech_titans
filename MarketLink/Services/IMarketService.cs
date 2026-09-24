using MarketLink.Models;
using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Services
{
    public interface IMarketService
    {
        Task<MarketIndexViewModel> GetMarketsAsync(MarketFilterViewModel filter);
        Task<MarketDetailsViewModel?> GetMarketDetailsAsync(int marketId);
        Task<MarketEditViewModel?> GetMarketForEditAsync(int marketId);
        Task<int> CreateMarketAsync(MarketCreateViewModel model);
        Task<bool> UpdateMarketAsync(MarketEditViewModel model);
        Task<bool> ChangeStatusAsync(int marketId, MarketStatus newStatus);
        Task<(bool success, int farmerCount)> DeleteMarketAsync(int marketId);
    }
}
