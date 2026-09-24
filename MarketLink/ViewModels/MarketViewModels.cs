using System.ComponentModel.DataAnnotations;
using MarketLink.Models;

namespace MarketLink.ViewModels
{
    /// <summary>
    /// Filter/search criteria posted from the Markets/Index toolbar.
    /// </summary>
    public class MarketFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public MarketStatus? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class MarketIndexViewModel
    {
        public MarketFilterViewModel Filter { get; set; } = new();
        public PagedResult<Market> Result { get; set; } = new();

        public int TotalMarkets { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
    }

    public class MarketDetailsViewModel
    {
        public Market Market { get; set; } = null!;
        public List<Farmer> Farmers { get; set; } = new();
        public int TotalFarmers { get; set; }
        public int TotalProducts { get; set; }
    }

    /// <summary>
    /// Shared field set for Create and Edit forms.
    /// </summary>
    public abstract class MarketFormViewModel
    {
        [Required, MaxLength(100)]
        [Display(Name = "Market Name")]
        public string MarketName { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [MaxLength(150)]
        [Display(Name = "Operating Days")]
        public string? OperatingDays { get; set; }

        [Display(Name = "Opening Time")]
        [DataType(DataType.Time)]
        public TimeSpan? OpeningTime { get; set; }

        [Display(Name = "Closing Time")]
        [DataType(DataType.Time)]
        public TimeSpan? ClosingTime { get; set; }

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        [Display(Name = "Latitude")]
        public decimal? Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        [Display(Name = "Longitude")]
        public decimal? Longitude { get; set; }

        [Required, MaxLength(30)]
        [Display(Name = "Map Provider")]
        public string MapProvider { get; set; } = "OpenStreetMap";

        [MaxLength(300)]
        [Display(Name = "Map Link")]
        [Url(ErrorMessage = "Enter a valid URL.")]
        public string? MapLink { get; set; }

        public MarketStatus Status { get; set; } = MarketStatus.Active;
    }

    public class MarketCreateViewModel : MarketFormViewModel
    {
    }

    public class MarketEditViewModel : MarketFormViewModel
    {
        public int MarketId { get; set; }
    }
}
