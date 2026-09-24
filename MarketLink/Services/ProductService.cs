using Microsoft.EntityFrameworkCore;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Services
{
    public class ProductService : IProductService
    {
        private readonly MarketLinkDbContext _context;

        public ProductService(MarketLinkDbContext context)
        {
            _context = context;
        }

        public async Task<ProductIndexViewModel> GetProductsAsync(ProductFilterViewModel filter)
        {
            var query = _context.Products
                .Include(p => p.Farmer)
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    (p.Farmer != null && p.Farmer.StallName.ToLower().Contains(term)));
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(p => p.Status == filter.Status.Value);
            }

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
            }

            if (filter.FarmerId.HasValue)
            {
                query = query.Where(p => p.FarmerId == filter.FarmerId.Value);
            }

            query = filter.SortBy switch
            {
                "name" => query.OrderBy(p => p.Name),
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "stock" => query.OrderBy(p => p.StockQuantity),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var totalCount = await query.CountAsync();

            var pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;
            var pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new ProductIndexViewModel
            {
                Filter = filter,
                Result = new PagedResult<Product>
                {
                    Items = items,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount
                },
                Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync(),
                Farmers = await _context.Farmers.OrderBy(f => f.StallName).ToListAsync(),
                TotalProducts = await _context.Products.CountAsync(),
                ActiveCount = await _context.Products.CountAsync(p => p.Status == ProductStatus.Active),
                SoldOutCount = await _context.Products.CountAsync(p => p.Status == ProductStatus.SoldOut),
                RemovedCount = await _context.Products.CountAsync(p => p.Status == ProductStatus.Removed)
            };

            return vm;
        }

        public async Task<ProductDetailsViewModel?> GetProductDetailsAsync(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Farmer)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
            {
                return null;
            }

            var reviews = await _context.Reviews
                .Include(r => r.Customer)
                .Where(r => r.ProductId == productId && !r.IsRemoved)
                .OrderByDescending(r => r.ReviewDate)
                .Take(10)
                .ToListAsync();

            var allRatings = await _context.Reviews
                .Where(r => r.ProductId == productId && !r.IsRemoved)
                .Select(r => r.Rating)
                .ToListAsync();

            var orderItems = await _context.OrderItems
                .Where(oi => oi.ProductId == productId)
                .ToListAsync();

            return new ProductDetailsViewModel
            {
                Product = product,
                Reviews = reviews,
                AverageRating = allRatings.Any() ? allRatings.Average() : 0,
                TotalUnitsOrdered = orderItems.Sum(oi => oi.Quantity),
                TotalOrders = orderItems.Select(oi => oi.OrderId).Distinct().Count()
            };
        }

        public async Task<bool> ChangeStatusAsync(int productId, ProductStatus newStatus)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return false;
            }

            product.Status = newStatus;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
