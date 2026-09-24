using MarketLink.Models;

namespace MarketLink.ViewModels
{
    /// <summary>
    /// Filter/search criteria posted from the Orders/Index toolbar.
    /// </summary>
    public class OrderFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public OrderStatus? Status { get; set; }
        public int? FarmerId { get; set; }

        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        public DateTime? DateFrom { get; set; }

        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        public DateTime? DateTo { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class OrderIndexViewModel
    {
        public OrderFilterViewModel Filter { get; set; } = new();
        public PagedResult<Order> Result { get; set; } = new();
        public List<Farmer> Farmers { get; set; } = new();

        public int TotalOrders { get; set; }
        public int PlacedCount { get; set; }
        public int AcceptedCount { get; set; }
        public int ReadyForPickupCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class OrderDetailsViewModel
    {
        public Order Order { get; set; } = null!;
    }
}
