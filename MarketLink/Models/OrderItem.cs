using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class OrderItem
    {
        [Key]
        public int OrderItemId { get; set; }

        public int OrderId { get; set; }
        [ForeignKey(nameof(OrderId))]
        public Order? Order { get; set; }

        public int ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal LineTotal { get; set; }

        [NotMapped] public int Id => OrderItemId;
        [NotMapped] public int QuantityKg { get => Quantity; set => Quantity = value; }
        [NotMapped] public decimal PricePerKgSnapshot { get => UnitPrice; set => UnitPrice = value; }
        [NotMapped] public decimal TotalPrice { get => LineTotal; set => LineTotal = value; }
        [NotMapped] public string ProductNameSnapshot { get; set; } = string.Empty;
    }
}
