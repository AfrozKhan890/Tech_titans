using System.ComponentModel.DataAnnotations;

namespace MarketLink.Areas.Admin.ViewModels
{
    /// <summary>
    /// Form-facing view model for the Settings page. Splits the fields
    /// into clearly named groups so the view stays readable and each
    /// group can be validated independently.
    /// </summary>
    public class SettingsViewModel
    {
        // ---------- General ----------

        [Required, MaxLength(100)]
        [Display(Name = "Platform Name")]
        public string PlatformName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        [EmailAddress]
        [Display(Name = "Contact Email")]
        public string ContactEmail { get; set; } = string.Empty;

        [MaxLength(30)]
        [Display(Name = "Contact Phone")]
        public string? ContactPhone { get; set; }

        [Required, MaxLength(50)]
        [Display(Name = "Default Map Provider")]
        public string DefaultMapProvider { get; set; } = "OpenStreetMap";

        // ---------- Notifications ----------

        [Display(Name = "New order notifications")]
        public bool EnableOrderNotifications { get; set; }

        [Display(Name = "New review notifications")]
        public bool EnableReviewNotifications { get; set; }

        [Display(Name = "Farmer registration notifications")]
        public bool EnableFarmerNotifications { get; set; }

        // ---------- SMTP ----------

        [Required, MaxLength(150)]
        [Display(Name = "SMTP Host")]
        public string SmtpHost { get; set; } = "smtp.gmail.com";

        [Range(1, 65535)]
        [Display(Name = "SMTP Port")]
        public int SmtpPort { get; set; } = 587;

        [Required, MaxLength(150)]
        [Display(Name = "SMTP Username")]
        public string SmtpUsername { get; set; } = string.Empty;

        [MaxLength(300)]
        [DataType(DataType.Password)]
        [Display(Name = "SMTP Password")]
        public string? SmtpPassword { get; set; }

        [Required, MaxLength(150)]
        [EmailAddress]
        [Display(Name = "From Email")]
        public string FromEmail { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        [Display(Name = "From Name")]
        public string FromName { get; set; } = string.Empty;

        [Display(Name = "Use SSL / STARTTLS")]
        public bool SmtpUseSsl { get; set; } = true;

        // Verification target — if set, the page will show a "Send test email"
        // panel that fires a real SMTP message through the configured server.
        [EmailAddress]
        [Display(Name = "Send a test email to")]
        public string? TestRecipient { get; set; }
    }

    /// <summary>
    /// Posted by the "Change Password" form on the Settings page.
    /// Kept separate from SettingsViewModel so the two forms can be
    /// validated independently.
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm the new password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "The new passwords do not match.")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}