using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        public int FarmerId { get; set; }
        [ForeignKey(nameof(FarmerId))]
        public Farmer? Farmer { get; set; }

        public int? CategoryId { get; set; }
        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [MaxLength(20)]
        public string Unit { get; set; } = "kg";

        public int StockQuantity { get; set; }

        [MaxLength(300)]
        public string? ImageUrl { get; set; }

        public ProductStatus Status { get; set; } = ProductStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [MaxLength(220)] public string? Slug { get; set; }
        [MaxLength(1000)] public string? Tags { get; set; }
        public bool IsOrganic { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsBestSeller { get; set; }
        public Season Season { get; set; } = Season.AllYear;

        [NotMapped] public int Id => ProductId;
        [NotMapped] public decimal PricePerKg => Price;
        [NotMapped] public int StockKg => StockQuantity;
        [NotMapped] public int StockQuantityKg { get => StockQuantity; set => StockQuantity = value; }
        [NotMapped] public bool IsAvailable => Status == ProductStatus.Active && StockQuantity > 0;


        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}


namespace MarketLink.Models
{
    public class ProductImage
    {
        [Key] public int ProductImageId { get; set; }
        public int ProductId { get; set; }
        [MaxLength(500)] public string ImageUrl { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; }
        public Product? Product { get; set; }
        [NotMapped] public int Id => ProductImageId;
    }
}
