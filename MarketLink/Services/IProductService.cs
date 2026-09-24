using MarketLink.Models;
using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Services
{
    public interface IProductService
    {
        Task<ProductIndexViewModel> GetProductsAsync(ProductFilterViewModel filter);
        Task<ProductDetailsViewModel?> GetProductDetailsAsync(int productId);
        Task<bool> ChangeStatusAsync(int productId, ProductStatus newStatus);
    }
}
