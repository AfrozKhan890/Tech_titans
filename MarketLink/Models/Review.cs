using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }

        public int CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        public int? ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        public int? FarmerId { get; set; }
        [ForeignKey(nameof(FarmerId))]
        public Farmer? Farmer { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; }

        public bool IsRemoved { get; set; } = false;

        public DateTime ReviewDate { get; set; } = DateTime.UtcNow;
        [MaxLength(150)] public string? Title { get; set; }
        [MaxLength(2000)] public string? FarmerResponse { get; set; }
        public DateTime? FarmerRespondedAt { get; set; }
        public bool IsApproved { get; set; } = true;

        [NotMapped] public int Id => ReviewId;
        [NotMapped] public DateTime CreatedAt { get => ReviewDate; set => ReviewDate = value; }
    }
}
