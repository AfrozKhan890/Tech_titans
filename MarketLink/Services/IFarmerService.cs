using MarketLink.Models;
using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Services
{
    public interface IFarmerService
    {
        Task<FarmerIndexViewModel> GetFarmersAsync(FarmerFilterViewModel filter);
        Task<FarmerDetailsViewModel?> GetFarmerDetailsAsync(int farmerId);
        Task<FarmerEditViewModel?> GetFarmerForEditAsync(int farmerId);
        Task<bool> UpdateFarmerAsync(FarmerEditViewModel model);
        Task<bool> ChangeStatusAsync(int farmerId, FarmerStatus newStatus);
        Task<List<Market>> GetMarketsAsync();
    }
}
