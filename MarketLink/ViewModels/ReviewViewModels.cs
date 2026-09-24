using MarketLink.Models;

namespace MarketLink.ViewModels
{
    /// <summary>
    /// Filter/search criteria posted from the Reviews/Index toolbar.
    /// </summary>
    public class ReviewFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public int? Rating { get; set; }
        public int? FarmerId { get; set; }

        // null = all, true = removed only, false = visible only
        public bool? IsRemoved { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class ReviewIndexViewModel
    {
        public ReviewFilterViewModel Filter { get; set; } = new();
        public PagedResult<Review> Result { get; set; } = new();
        public List<Farmer> Farmers { get; set; } = new();

        public int TotalReviews { get; set; }
        public int VisibleCount { get; set; }
        public int RemovedCount { get; set; }
        public double AverageRating { get; set; }
    }

    public class ReviewDetailsViewModel
    {
        public Review Review { get; set; } = null!;
    }
}
