using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Areas.Admin.Services
{
    public interface ISettingsService
    {
        Task<SettingsViewModel> GetSettingsAsync();
        Task UpdateSettingsAsync(SettingsViewModel model);
        Task<bool> SendTestEmailAsync(string toEmail);
    }
}