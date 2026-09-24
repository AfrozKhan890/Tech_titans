using System.ComponentModel.DataAnnotations;

namespace MarketLink.Models
{
    /// <summary>
    /// Single-row settings table for the MarketLink Admin platform.
    /// Kept intentionally narrow — only what the SRS requires for basic
    /// platform configuration plus outbound SMTP for verification emails.
    /// </summary>
    public class PlatformSettings
    {
        [Key]
        public int SettingsId { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Platform Name")]
        public string PlatformName { get; set; } = "MarketLink";

        [Required, MaxLength(100)]
        [EmailAddress]
        [Display(Name = "Contact Email")]
        public string ContactEmail { get; set; } = "admin@marketlink.com";

        [MaxLength(30)]
        [Display(Name = "Contact Phone")]
        public string? ContactPhone { get; set; }

        [MaxLength(50)]
        [Display(Name = "Default Map Provider")]
        public string DefaultMapProvider { get; set; } = "OpenStreetMap";

        [Display(Name = "Enable New Order Notifications")]
        public bool EnableOrderNotifications { get; set; } = true;

        [Display(Name = "Enable New Review Notifications")]
        public bool EnableReviewNotifications { get; set; } = true;

        [Display(Name = "Enable Farmer Registration Notifications")]
        public bool EnableFarmerNotifications { get; set; } = true;

        // ---------- SMTP configuration (for verification emails) ----------

        [MaxLength(150)]
        [Display(Name = "SMTP Host")]
        public string SmtpHost { get; set; } = "smtp.gmail.com";

        [Display(Name = "SMTP Port")]
        public int SmtpPort { get; set; } = 587;

        [MaxLength(150)]
        [Display(Name = "SMTP Username")]
        public string SmtpUsername { get; set; } = string.Empty;

        /// <summary>
        /// Stored plaintext here because it is a shared mailbox credential
        /// used only for outbound notifications. If your deployment policy
        /// requires it, encrypt this field at rest via DPAPI / KeyVault.
        /// </summary>
        [MaxLength(300)]
        [DataType(DataType.Password)]
        [Display(Name = "SMTP Password")]
        public string? SmtpPassword { get; set; }

        [MaxLength(150)]
        [EmailAddress]
        [Display(Name = "From Email")]
        public string FromEmail { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "From Name")]
        public string FromName { get; set; } = "MarketLink";

        [Display(Name = "Enable SSL (STARTTLS on port 587)")]
        public bool SmtpUseSsl { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}