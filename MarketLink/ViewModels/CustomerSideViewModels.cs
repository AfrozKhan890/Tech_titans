using System.ComponentModel.DataAnnotations;
using MarketLink.Models;

namespace MarketLink.ViewModels
{
    public class CustomerProfileViewModel
    {
        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        [Display(Name = "Phone Number")] public string? PhoneNumber { get; set; }
        [Display(Name = "About Me")] public string? Bio { get; set; }
        [Display(Name = "Default City")] public string? DefaultCity { get; set; }
        public string? ProfileImageUrl { get; set; }
        public DateTime MemberSince { get; set; }
        public int TotalOrders { get; set; }
        public int TotalFavorites { get; set; }
        public int TotalAddresses { get; set; }
        public IEnumerable<CustomerAddress> Addresses { get; set; } = [];
    }

    public class CustomerAddressViewModel
    {
        public int Id { get; set; }
        [Required] public string Label { get; set; } = "Home";
        [Required, Display(Name = "Address Line 1")] public string AddressLine1 { get; set; } = string.Empty;
        [Display(Name = "Address Line 2")] public string? AddressLine2 { get; set; }
        [Required] public string City { get; set; } = string.Empty;
        [Required] public string State { get; set; } = string.Empty;
        [Required, Display(Name = "Postal Code")] public string PostalCode { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }

    public class DashboardViewModel
    {
        public string CustomerName { get; set; } = string.Empty;
        public string? DefaultCity { get; set; }
        public int TotalOrdersCount { get; set; }
        public int TotalFavoritesCount { get; set; }
        public int TotalAddressesCount { get; set; }
        public decimal TotalSpent { get; set; }
        public IEnumerable<Product> Products { get; set; } = [];
        public IEnumerable<Category> Categories { get; set; } = [];
        public IEnumerable<Favorite> Favorites { get; set; } = [];
        public IEnumerable<Order> RecentOrders { get; set; } = [];
        public HashSet<int> FavoriteProductIds { get; set; } = [];
    }

    public class CustomerReviewCreateViewModel
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        [Range(1, 5)] public int Rating { get; set; } = 5;
        [MaxLength(150)] public string? Title { get; set; }
        [MaxLength(2000)] public string? Comment { get; set; }
    }

    public class CustomerLoginViewModel
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
        public string? ReturnUrl { get; set; }
    }

    public class CustomerRegisterViewModel
    {
        [Required, MaxLength(100)] public string FullName { get; set; } = string.Empty;
        [Required, EmailAddress, MaxLength(100)] public string Email { get; set; } = string.Empty;
        [MaxLength(20)] public string? Phone { get; set; }
        [Required, MinLength(8), DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
        [Required, DataType(DataType.Password), Compare(nameof(Password))] public string ConfirmPassword { get; set; } = string.Empty;
        [MaxLength(100)] public string? City { get; set; }
    }
}
