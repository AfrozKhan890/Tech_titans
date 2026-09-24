using System.ComponentModel.DataAnnotations;

namespace MarketLink.Models
{
    /// <summary>
    /// A log record of a generated report (used by the Reports/Analytics phase).
    /// Included now so the DbContext relationships described in the SRS are
    /// complete from Phase 1 onward.
    /// </summary>
    public class PlatformReport
    {
        [Key]
        public int ReportId { get; set; }

        public int GeneratedByAdminId { get; set; }

        [MaxLength(50)]
        public string ReportType { get; set; } = string.Empty;

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
