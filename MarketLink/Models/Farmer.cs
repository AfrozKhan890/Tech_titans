using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class Farmer
    {
        [Key]
        public int FarmerId { get; set; }

        [Required, MaxLength(100)]
        public string StallName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string ContactPerson { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Address { get; set; }

        public FarmerStatus Status { get; set; } = FarmerStatus.Pending;

        [MaxLength(150)]
        public string? OperatingDays { get; set; }

        public TimeSpan? PickupWindowStart { get; set; }
        public TimeSpan? PickupWindowEnd { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        // Customer-facing compatibility aliases.
        [NotMapped] public int Id => FarmerId;
        [NotMapped] public string FarmName => StallName;
        [MaxLength(1000)]
        public string? Description { get; set; }
        [NotMapped] public string? Bio => Description;
        public bool IsFeatured { get; set; }
        [NotMapped] public string? UserId => FarmerId.ToString();

        // Navigation
        public int? MarketId { get; set; }
        [ForeignKey(nameof(MarketId))]
        public Market? Market { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    }
}
