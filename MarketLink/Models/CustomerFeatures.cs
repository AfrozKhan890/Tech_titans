using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class CustomerAddress
    {
        [Key] public int Id { get; set; }
        public int CustomerId { get; set; }
        [MaxLength(50)] public string Label { get; set; } = "Home";
        [Required, MaxLength(200)] public string AddressLine1 { get; set; } = string.Empty;
        [MaxLength(200)] public string? AddressLine2 { get; set; }
        [Required, MaxLength(100)] public string City { get; set; } = string.Empty;
        [Required, MaxLength(100)] public string State { get; set; } = string.Empty;
        [Required, MaxLength(20)] public string PostalCode { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public Customer? Customer { get; set; }
    }

    public class CartItem
    {
        [Key] public int Id { get; set; }
        public int CustomerId { get; set; }
        public int ProductId { get; set; }
        public int QuantityKg { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        public Customer? Customer { get; set; }
        public Product? Product { get; set; }
    }

    public class Favorite
    {
        [Key] public int Id { get; set; }
        public int CustomerId { get; set; }
        public int? ProductId { get; set; }
        public int? FarmerId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Customer? Customer { get; set; }
        public Product? Product { get; set; }
        public Farmer? Farmer { get; set; }
    }

    public class Notification
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(50)] public string UserId { get; set; } = string.Empty;
        [Required, MaxLength(100)] public string Title { get; set; } = string.Empty;
        [Required] public string Message { get; set; } = string.Empty;
        [MaxLength(300)] public string? Link { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
