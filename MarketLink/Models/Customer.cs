using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }

        public CustomerStatus Status { get; set; } = CustomerStatus.Active;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        // Customer authentication/profile fields. Admin accounts remain in AdminUser.
        public string? PasswordHash { get; set; }
        public string? PasswordSalt { get; set; }
        [MaxLength(500)]
        public string? Bio { get; set; }
        [MaxLength(100)]
        public string? DefaultCity { get; set; }
        [MaxLength(300)]
        public string? ProfileImageUrl { get; set; }

        // Compatibility with the Customer-side implementation.
        [NotMapped] public int Id => CustomerId;
        [NotMapped] public string? UserId => CustomerId.ToString();

        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
