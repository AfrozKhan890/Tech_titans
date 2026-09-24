using System.ComponentModel.DataAnnotations;

namespace MarketLink.Models
{
    /// <summary>
    /// Represents a MarketLink platform administrator.
    /// Kept separate from Farmer/Customer accounts per the SRS requirement
    /// for a dedicated Admin login/dashboard, distinct from Customer and Farmer views.
    /// </summary>
    public class AdminUser
    {
        [Key]
        public int AdminId { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Salted hash of the password. Never store plain-text passwords.
        /// </summary>
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string PasswordSalt { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Role { get; set; } = "SuperAdmin";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }
    }
}
