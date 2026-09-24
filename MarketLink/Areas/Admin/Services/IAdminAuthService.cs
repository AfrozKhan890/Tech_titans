using MarketLink.Models;

namespace MarketLink.Areas.Admin.Services
{
    public interface IAdminAuthService
    {
        Task<AdminUser?> ValidateCredentialsAsync(string email, string password);
        Task UpdateLastLoginAsync(int adminId);
    }
}
