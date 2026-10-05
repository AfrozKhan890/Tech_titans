using System.ComponentModel.DataAnnotations;
using MarketLink.Models.Enums;

namespace MarketLink.Models;



public class ProductInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Produce name is required.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a category.")]
    public int CategoryId { get; set; }

    [Range(0.01, 100000, ErrorMessage = "Price must be greater than zero.")]
    public decimal PricePerKg { get; set; }

    [Range(0, 1000000, ErrorMessage = "Stock cannot be negative.")]
    public int StockQuantityKg { get; set; }

    [Required(ErrorMessage = "Unit is required.")]
    [StringLength(20)]
    public string Unit { get; set; } = "KG";

    [StringLength(4000)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    [StringLength(300)]
    public string? Tags { get; set; }

    public Season Season { get; set; } = Season.AllYear;

    public bool IsOrganic { get; set; }
    public bool IsAvailable { get; set; } = true;


    public int? FarmerId { get; set; }



    [DataType(DataType.Upload)]
    public IFormFile? ImageFile { get; set; }
}
