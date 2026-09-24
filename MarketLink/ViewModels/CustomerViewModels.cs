using MarketLink.Models;

namespace MarketLink.ViewModels
{
    /// <summary>
    /// Filter/search criteria posted from the Customers/Index toolbar.
    /// </summary>
    public class CustomerFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public CustomerStatus? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class CustomerIndexViewModel
    {
        public CustomerFilterViewModel Filter { get; set; } = new();
        public PagedResult<Customer> Result { get; set; } = new();

        public int TotalCustomers { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
        public int NewThisMonthCount { get; set; }
    }

    public class CustomerDetailsViewModel
    {
        public Customer Customer { get; set; } = null!;
        public List<Order> Orders { get; set; } = new();
        public List<Review> Reviews { get; set; } = new();

        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
    }
}
