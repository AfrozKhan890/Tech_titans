using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        public int CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        public int FarmerId { get; set; }
        [ForeignKey(nameof(FarmerId))]
        public Farmer? Farmer { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        public OrderStatus OrderStatus { get; set; } = OrderStatus.Placed;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public DateOnly? PickupDate { get; set; }
        public TimeSpan? PickupTime { get; set; }
        public int? MarketId { get; set; }
        [ForeignKey(nameof(MarketId))] public Market? Market { get; set; }

        [MaxLength(30)] public string? OrderNumber { get; set; }
        [MaxLength(50)] public string PaymentMethod { get; set; } = "Cash on Pickup";
        [MaxLength(1000)] public string? Notes { get; set; }
        [MaxLength(1000)] public string? FarmerNotes { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? ReadyAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        [MaxLength(500)] public string? CancellationReason { get; set; }

        [NotMapped] public int Id => OrderId;
        [NotMapped] public OrderStatus Status { get => OrderStatus; set => OrderStatus = value; }
        [NotMapped] public DateTime OrderedAt { get => OrderDate; set => OrderDate = value; }
        [NotMapped] public decimal SubTotal { get => TotalAmount; set => TotalAmount = value; }
        [NotMapped] public ICollection<OrderItem> Items => OrderItems;

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
