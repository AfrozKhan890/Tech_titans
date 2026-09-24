using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Services
{
    public interface ICategoryService
    {
        Task<CategoryIndexViewModel> GetCategoriesAsync(CategoryFilterViewModel filter);
        Task<CategoryEditViewModel?> GetForEditAsync(int categoryId);
        Task<int> CreateAsync(CategoryCreateViewModel model);
        Task<bool> UpdateAsync(CategoryEditViewModel model);
        Task<bool> ChangeStatusAsync(int categoryId, bool isActive);
        Task<(bool success, string? error)> DeleteAsync(int categoryId);
    }
}