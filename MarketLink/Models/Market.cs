using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class Market
    {
        [Key]
        public int MarketId { get; set; }

        [Required, MaxLength(100)]
        public string MarketName { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? OperatingDays { get; set; }

        public TimeSpan? OpeningTime { get; set; }
        public TimeSpan? ClosingTime { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        [MaxLength(30)]
        public string MapProvider { get; set; } = "OpenStreetMap";

        [MaxLength(300)]
        public string? MapLink { get; set; }

        public MarketStatus Status { get; set; } = MarketStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Customer-facing compatibility aliases.
        [NotMapped] public int Id => MarketId;
        [NotMapped] public string Name => MarketName;
        [MaxLength(1000)] public string? Description { get; set; }
        [MaxLength(100)] public string City { get; set; } = string.Empty;
        [MaxLength(100)] public string State { get; set; } = string.Empty;
        [MaxLength(300)] public string? ImageUrl { get; set; }
        [NotMapped] public string? OperatingHours => OpeningTime.HasValue && ClosingTime.HasValue
            ? $"{OpeningTime:hh\\:mm} - {ClosingTime:hh\\:mm}" : null;
        [MaxLength(30)] public string? Phone { get; set; }
        [MaxLength(150)] public string? Email { get; set; }
        [MaxLength(300)] public string? Website { get; set; }
        [NotMapped] public bool IsActive => Status == MarketStatus.Active;

        // Navigation
        public ICollection<Farmer> Farmers { get; set; } = new List<Farmer>();
    }
}
