using MarketLink.Models;

namespace MarketLink.ViewModels
{
    /// <summary>
    /// Filter/search/sort criteria posted from the Products/Index toolbar.
    /// </summary>
    public class ProductFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public ProductStatus? Status { get; set; }
        public int? CategoryId { get; set; }
        public int? FarmerId { get; set; }

        /// <summary>
        /// One of: newest, name, price_asc, price_desc, stock
        /// </summary>
        public string SortBy { get; set; } = "newest";

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class ProductIndexViewModel
    {
        public ProductFilterViewModel Filter { get; set; } = new();
        public PagedResult<Product> Result { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<Farmer> Farmers { get; set; } = new();

        public int TotalProducts { get; set; }
        public int ActiveCount { get; set; }
        public int SoldOutCount { get; set; }
        public int RemovedCount { get; set; }
    }

    public class ProductDetailsViewModel
    {
        public Product Product { get; set; } = null!;
        public List<Review> Reviews { get; set; } = new();

        public double AverageRating { get; set; }
        public int TotalUnitsOrdered { get; set; }
        public int TotalOrders { get; set; }
    }
}
