using Microsoft.EntityFrameworkCore;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Services
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly MarketLinkDbContext _context;

        public AnnouncementService(MarketLinkDbContext context)
        {
            _context = context;
        }

        public async Task<AnnouncementIndexViewModel> GetAnnouncementsAsync(AnnouncementFilterViewModel filter)
        {
            var query = _context.Announcements.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(a =>
                    a.Title.ToLower().Contains(term) ||
                    a.Message.ToLower().Contains(term));
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(a => a.Status == filter.Status.Value);
            }

            var totalCount = await query.CountAsync();

            var pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;
            var pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;

            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new AnnouncementIndexViewModel
            {
                Filter = filter,
                Result = new PagedResult<Announcement>
                {
                    Items = items,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount
                },
                TotalAnnouncements = await _context.Announcements.CountAsync(),
                PublishedCount = await _context.Announcements.CountAsync(a => a.Status == AnnouncementStatus.Published),
                DraftCount = await _context.Announcements.CountAsync(a => a.Status == AnnouncementStatus.Draft),
                UnpublishedCount = await _context.Announcements.CountAsync(a => a.Status == AnnouncementStatus.Unpublished)
            };
        }

        public async Task<AnnouncementDetailsViewModel?> GetDetailsAsync(int announcementId)
        {
            var a = await _context.Announcements.FirstOrDefaultAsync(x => x.AnnouncementId == announcementId);
            if (a == null)
            {
                return null;
            }

            var admin = await _context.AdminUsers.FirstOrDefaultAsync(u => u.AdminId == a.CreatedByAdminId);

            return new AnnouncementDetailsViewModel
            {
                Announcement = a,
                CreatedBy = admin
            };
        }

        public async Task<AnnouncementEditViewModel?> GetForEditAsync(int announcementId)
        {
            var a = await _context.Announcements.FirstOrDefaultAsync(x => x.AnnouncementId == announcementId);
            if (a == null)
            {
                return null;
            }

            return new AnnouncementEditViewModel
            {
                AnnouncementId = a.AnnouncementId,
                Title = a.Title,
                Message = a.Message,
                Status = a.Status,
                ExpiryAt = a.ExpiryAt,
                CreatedAt = a.CreatedAt,
                PublishedAt = a.PublishedAt,
                CreatedByAdminId = a.CreatedByAdminId
            };
        }

        public async Task<int> CreateAsync(AnnouncementCreateViewModel model, int adminId)
        {
            var announcement = new Announcement
            {
                Title = model.Title.Trim(),
                Message = model.Message.Trim(),
                Status = model.Status,
                ExpiryAt = model.ExpiryAt,
                CreatedAt = DateTime.UtcNow,
                CreatedByAdminId = adminId
            };

            if (announcement.Status == AnnouncementStatus.Published)
            {
                announcement.PublishedAt = DateTime.UtcNow;
            }

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();
            return announcement.AnnouncementId;
        }

        public async Task<bool> UpdateAsync(AnnouncementEditViewModel model)
        {
            var a = await _context.Announcements.FindAsync(model.AnnouncementId);
            if (a == null)
            {
                return false;
            }

            // If transitioning into Published and not already published, stamp time.
            if (model.Status == AnnouncementStatus.Published && a.Status != AnnouncementStatus.Published)
            {
                a.PublishedAt = DateTime.UtcNow;
            }

            a.Title = model.Title.Trim();
            a.Message = model.Message.Trim();
            a.Status = model.Status;
            a.ExpiryAt = model.ExpiryAt;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangeStatusAsync(int announcementId, AnnouncementStatus newStatus)
        {
            var a = await _context.Announcements.FindAsync(announcementId);
            if (a == null)
            {
                return false;
            }

            if (newStatus == AnnouncementStatus.Published && a.Status != AnnouncementStatus.Published)
            {
                a.PublishedAt = DateTime.UtcNow;
            }

            a.Status = newStatus;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int announcementId)
        {
            var a = await _context.Announcements.FindAsync(announcementId);
            if (a == null)
            {
                return false;
            }

            _context.Announcements.Remove(a);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}