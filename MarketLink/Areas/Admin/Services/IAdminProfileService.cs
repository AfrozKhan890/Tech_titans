using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Areas.Admin.Services
{
    public interface IAdminProfileService
    {
        Task<AdminProfileViewModel?> GetProfileAsync(int adminId);
        Task<(bool success, string? error)> UpdateProfileAsync(AdminProfileViewModel model);
        Task<(bool success, string? error)> ChangePasswordAsync(int adminId, ChangePasswordViewModel model);
    }
}