using System.ComponentModel.DataAnnotations;
using MarketLink.Models;

namespace MarketLink.ViewModels
{
    public class AnnouncementFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public AnnouncementStatus? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class AnnouncementIndexViewModel
    {
        public AnnouncementFilterViewModel Filter { get; set; } = new();
        public PagedResult<Announcement> Result { get; set; } = new();

        public int TotalAnnouncements { get; set; }
        public int PublishedCount { get; set; }
        public int DraftCount { get; set; }
        public int UnpublishedCount { get; set; }
    }

    public abstract class AnnouncementFormViewModel
    {
        [Required, MaxLength(150)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Message")]
        [DataType(DataType.MultilineText)]
        public string Message { get; set; } = string.Empty;

        [Display(Name = "Status")]
        public AnnouncementStatus Status { get; set; } = AnnouncementStatus.Draft;

        [Display(Name = "Expires On")]
        [DataType(DataType.DateTime)]
        public DateTime? ExpiryAt { get; set; }
    }

    public class AnnouncementCreateViewModel : AnnouncementFormViewModel
    {
    }

    public class AnnouncementEditViewModel : AnnouncementFormViewModel
    {
        public int AnnouncementId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public int CreatedByAdminId { get; set; }
    }

    public class AnnouncementDetailsViewModel
    {
        public Announcement Announcement { get; set; } = null!;
        public AdminUser? CreatedBy { get; set; }
    }
}