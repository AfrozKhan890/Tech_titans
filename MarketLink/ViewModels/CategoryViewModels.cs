using System.ComponentModel.DataAnnotations;
using MarketLink.Models;

namespace MarketLink.ViewModels
{
    public class CategoryFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class CategoryIndexViewModel
    {
        public CategoryFilterViewModel Filter { get; set; } = new();
        public PagedResult<Category> Result { get; set; } = new();

        public int TotalCategories { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
    }

    public abstract class CategoryFormViewModel
    {
        [Required, MaxLength(50)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(250)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    public class CategoryCreateViewModel : CategoryFormViewModel
    {
    }

    public class CategoryEditViewModel : CategoryFormViewModel
    {
        public int CategoryId { get; set; }

        [Display(Name = "Products using this category")]
        public int ProductCount { get; set; }

        [Display(Name = "Created")]
        public DateTime CreatedAt { get; set; }
    }
}