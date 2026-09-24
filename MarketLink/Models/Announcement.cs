using System.ComponentModel.DataAnnotations;

namespace MarketLink.Models
{
    public class Announcement
    {
        [Key]
        public int AnnouncementId { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public AnnouncementStatus Status { get; set; } = AnnouncementStatus.Draft;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PublishedAt { get; set; }
        public DateTime? ExpiryAt { get; set; }

        public int CreatedByAdminId { get; set; }
    }
}
