using System.ComponentModel.DataAnnotations;
using MarketLink.Models;

namespace MarketLink.ViewModels
{
    /// <summary>
    /// Filter/search criteria posted from the Farmers/Index toolbar.
    /// </summary>
    public class FarmerFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public FarmerStatus? Status { get; set; }
        public int? MarketId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class FarmerIndexViewModel
    {
        public FarmerFilterViewModel Filter { get; set; } = new();
        public PagedResult<Farmer> Result { get; set; } = new();
        public List<Market> Markets { get; set; } = new();

        public int TotalFarmers { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int SuspendedCount { get; set; }
    }

    public class FarmerDetailsViewModel
    {
        public Farmer Farmer { get; set; } = null!;
        public List<Product> Products { get; set; } = new();
        public List<Order> RecentOrders { get; set; } = new();
        public List<Review> Reviews { get; set; } = new();

        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AverageRating { get; set; }
    }

    public class FarmerEditViewModel
    {
        public int FarmerId { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Stall / Business Name")]
        public string StallName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        [Display(Name = "Contact Person")]
        public string ContactPerson { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        [Display(Name = "Phone")]
        public string Phone { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [MaxLength(250)]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "Operating Days")]
        [MaxLength(150)]
        public string? OperatingDays { get; set; }

        [Display(Name = "Market")]
        public int? MarketId { get; set; }

        [Display(Name = "Pickup Window Start")]
        [DataType(DataType.Time)]
        public TimeSpan? PickupWindowStart { get; set; }

        [Display(Name = "Pickup Window End")]
        [DataType(DataType.Time)]
        public TimeSpan? PickupWindowEnd { get; set; }

        public FarmerStatus Status { get; set; }

        public List<Market> Markets { get; set; } = new();
    }
}
