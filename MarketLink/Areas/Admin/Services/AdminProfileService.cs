using Microsoft.EntityFrameworkCore;
using MarketLink.Data;
using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;
using MarketLink.Services;

namespace MarketLink.Areas.Admin.Services
{
    public class AdminProfileService : IAdminProfileService
    {
        private readonly MarketLinkDbContext _context;

        public AdminProfileService(MarketLinkDbContext context)
        {
            _context = context;
        }

        public async Task<AdminProfileViewModel?> GetProfileAsync(int adminId)
        {
            var admin = await _context.AdminUsers.FirstOrDefaultAsync(a => a.AdminId == adminId);
            if (admin == null)
            {
                return null;
            }

            return new AdminProfileViewModel
            {
                AdminId = admin.AdminId,
                FullName = admin.FullName,
                Email = admin.Email,
                Role = admin.Role,
                CreatedAt = admin.CreatedAt,
                LastLoginAt = admin.LastLoginAt
            };
        }

        public async Task<(bool success, string? error)> UpdateProfileAsync(AdminProfileViewModel model)
        {
            var admin = await _context.AdminUsers.FirstOrDefaultAsync(a => a.AdminId == model.AdminId);
            if (admin == null)
            {
                return (false, "Admin account not found.");
            }

            var emailInUse = await _context.AdminUsers
                .AnyAsync(a => a.AdminId != model.AdminId && a.Email.ToLower() == model.Email.ToLower());

            if (emailInUse)
            {
                return (false, "Another admin account already uses that email address.");
            }

            admin.FullName = model.FullName.Trim();
            admin.Email = model.Email.Trim();
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool success, string? error)> ChangePasswordAsync(int adminId, ChangePasswordViewModel model)
        {
            var admin = await _context.AdminUsers.FirstOrDefaultAsync(a => a.AdminId == adminId);
            if (admin == null)
            {
                return (false, "Admin account not found.");
            }

            if (!PasswordHasher.VerifyPassword(model.CurrentPassword, admin.PasswordHash, admin.PasswordSalt))
            {
                return (false, "Current password is incorrect.");
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                return (false, "New passwords do not match.");
            }

            var (hash, salt) = PasswordHasher.HashPassword(model.NewPassword);
            admin.PasswordHash = hash;
            admin.PasswordSalt = salt;
            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}