using MarketLink.Models;
using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Services
{
    public interface IAnnouncementService
    {
        Task<AnnouncementIndexViewModel> GetAnnouncementsAsync(AnnouncementFilterViewModel filter);
        Task<AnnouncementDetailsViewModel?> GetDetailsAsync(int announcementId);
        Task<AnnouncementEditViewModel?> GetForEditAsync(int announcementId);
        Task<int> CreateAsync(AnnouncementCreateViewModel model, int adminId);
        Task<bool> UpdateAsync(AnnouncementEditViewModel model);
        Task<bool> ChangeStatusAsync(int announcementId, AnnouncementStatus newStatus);
        Task<bool> DeleteAsync(int announcementId);
    }
}