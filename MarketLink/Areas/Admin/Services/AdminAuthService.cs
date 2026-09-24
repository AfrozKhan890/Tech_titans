using Microsoft.EntityFrameworkCore;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.Services;

namespace MarketLink.Areas.Admin.Services
{
    public class AdminAuthService : IAdminAuthService
    {
        private readonly MarketLinkDbContext _context;

        public AdminAuthService(MarketLinkDbContext context)
        {
            _context = context;
        }

        public async Task<AdminUser?> ValidateCredentialsAsync(string email, string password)
        {
            var admin = await _context.AdminUsers
                .FirstOrDefaultAsync(a => a.Email.ToLower() == email.ToLower() && a.IsActive);

            if (admin == null)
            {
                return null;
            }

            bool isValid = PasswordHasher.VerifyPassword(password, admin.PasswordHash, admin.PasswordSalt);
            return isValid ? admin : null;
        }

        public async Task UpdateLastLoginAsync(int adminId)
        {
            var admin = await _context.AdminUsers.FindAsync(adminId);
            if (admin != null)
            {
                admin.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}
