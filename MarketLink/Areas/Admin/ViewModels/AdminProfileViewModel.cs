using System.ComponentModel.DataAnnotations;

namespace MarketLink.Areas.Admin.ViewModels
{
    public class AdminProfileViewModel
    {
        public int AdminId { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}