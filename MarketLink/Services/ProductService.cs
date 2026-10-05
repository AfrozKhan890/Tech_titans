using MarketLink.Models;
using MarketLink.Models.Enums;
using MarketLink.Repositories;
using MarketLink.Services;
using MarketLink.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Services;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _db;
    private readonly ProductImageStore _imageStore;

    public ProductService(ApplicationDbContext db, ProductImageStore imageStore)
    {
        _db = db;
        _imageStore = imageStore;
    }

    public async Task<(IEnumerable<Product> Items, int TotalCount)> SearchAsync(
        string? query, int? categoryId, int? farmerId, Season? season,
        bool? isOrganic, decimal? minPrice, decimal? maxPrice,
        string? sortBy, int page, int pageSize)
    {
        var q = _db.Products
            .Include(p => p.Farmer).ThenInclude(f => f.User)
            .Include(p => p.Farmer).ThenInclude(f => f.FarmerMarkets).ThenInclude(fm => fm.Market)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Where(p => p.IsAvailable && p.StockQuantityKg > 0 && p.Farmer.Status == FarmerStatus.Approved && p.Farmer.FarmerMarkets.Any(fm => fm.IsActive && fm.Market.IsActive));

        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(p => p.Name.Contains(query) || p.Description!.Contains(query) || p.Tags!.Contains(query));
        if (categoryId.HasValue)
            q = q.Where(p => p.CategoryId == categoryId.Value || p.Category.ParentId == categoryId.Value);
        if (farmerId.HasValue)
            q = q.Where(p => p.FarmerId == farmerId.Value);
        if (season.HasValue)
            q = q.Where(p => p.Season == season.Value || p.Season == Season.AllYear);
        if (isOrganic.HasValue)
            q = q.Where(p => p.IsOrganic == isOrganic.Value);
        if (minPrice.HasValue)
            q = q.Where(p => p.PricePerKg >= minPrice.Value);
        if (maxPrice.HasValue)
            q = q.Where(p => p.PricePerKg <= maxPrice.Value);

        q = sortBy switch
        {
            "price_asc" => q.OrderBy(p => p.PricePerKg),
            "price_desc" => q.OrderByDescending(p => p.PricePerKg),
            "newest" => q.OrderByDescending(p => p.CreatedAt),
            "popular" => q.OrderByDescending(p => p.IsBestSeller).ThenByDescending(p => p.CreatedAt),
            _ => q.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAt)
        };

        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<Product?> GetBySlugAsync(string slug)
        => await _db.Products
            .Include(p => p.Farmer).ThenInclude(f => f.User)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Reviews).ThenInclude(r => r.Customer).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(p => p.Slug == slug);

    public async Task<IEnumerable<Product>> GetFeaturedAsync(int count = 8)
        => await _db.Products
            .Include(p => p.Farmer)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Where(p => p.IsFeatured && p.IsAvailable && p.StockQuantityKg > 0 && p.Farmer.Status == FarmerStatus.Approved && p.Farmer.FarmerMarkets.Any(fm => fm.IsActive && fm.Market.IsActive))
            .Take(count).ToListAsync();

    public async Task<IEnumerable<Product>> GetBestSellersAsync(int count = 8)
        => await _db.Products
            .Include(p => p.Farmer)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Where(p => p.IsBestSeller && p.IsAvailable && p.StockQuantityKg > 0 && p.Farmer.Status == FarmerStatus.Approved && p.Farmer.FarmerMarkets.Any(fm => fm.IsActive && fm.Market.IsActive))
            .Take(count).ToListAsync();

    public async Task<IEnumerable<Product>> GetSeasonalAsync(int count = 8)
    {
        var currentSeason = GetCurrentSeason();
        return await _db.Products
            .Include(p => p.Farmer)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Where(p => (p.Season == currentSeason || p.Season == Season.AllYear)
                        && p.IsAvailable && p.StockQuantityKg > 0 && p.Farmer.Status == FarmerStatus.Approved && p.Farmer.FarmerMarkets.Any(fm => fm.IsActive && fm.Market.IsActive))
            .Take(count).ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetByFarmerAsync(int farmerId)
        => await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Where(p => p.FarmerId == farmerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId)
        => await _db.Products
            .Include(p => p.Farmer)
            .Include(p => p.Images)
            .Where(p => p.CategoryId == categoryId && p.IsAvailable && p.StockQuantityKg > 0 && p.Farmer.Status == FarmerStatus.Approved && p.Farmer.FarmerMarkets.Any(fm => fm.IsActive && fm.Market.IsActive))
            .ToListAsync();

    public async Task<Product> CreateAsync(Product product, IEnumerable<string> imageUrls)
    {
        var urls = imageUrls.Where(u => !string.IsNullOrWhiteSpace(u)).Select(u => u.Trim()).ToList();

        product.Slug = GenerateSlug(product.Name, product.FarmerId);
        product.CreatedAt = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(product.ImageUrl) && urls.Count > 0) product.ImageUrl = urls[0];
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var images = urls.Select((url, idx) => new ProductImage
        {
            ProductId = product.Id,
            ImageUrl = url,
            IsPrimary = idx == 0,
            SortOrder = idx
        });
        _db.ProductImages.AddRange(images);
        await _db.SaveChangesAsync();
        return product;
    }

    public async Task<Product> UpdateAsync(Product product, IEnumerable<string>? newImageUrls = null)
    {
        product.UpdatedAt = DateTime.UtcNow;
        _db.Products.Update(product);

        var oldLocalImageUrls = new List<string>();
        if (newImageUrls != null)
        {
            var urls = newImageUrls.Where(u => !string.IsNullOrWhiteSpace(u)).Select(u => u.Trim()).ToList();
            oldLocalImageUrls = await _db.ProductImages
                .Where(i => i.ProductId == product.Id)
                .Select(i => i.ImageUrl)
                .ToListAsync();

            _db.ProductImages.RemoveRange(_db.ProductImages.Where(i => i.ProductId == product.Id));
            var images = urls.Select((url, idx) => new ProductImage
            {
                ProductId = product.Id,
                ImageUrl = url,
                IsPrimary = idx == 0,
                SortOrder = idx
            });
            _db.ProductImages.AddRange(images);

            product.ImageUrl = urls.FirstOrDefault();
        }

        await _db.SaveChangesAsync();

        if (newImageUrls != null)
        {
            var retained = newImageUrls.Where(u => !string.IsNullOrWhiteSpace(u)).Select(u => u.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var oldUrl in oldLocalImageUrls.Where(u => !retained.Contains(u)))
                _imageStore.DeleteIfLocal(oldUrl);
        }

        return product;
    }



    public async Task DeleteAsync(int productId)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null) return;

        var referenced = await _db.OrderItems.AnyAsync(i => i.ProductId == productId)
            || await _db.CartItems.AnyAsync(c => c.ProductId == productId)
            || await _db.Favorites.AnyAsync(f => f.ProductId == productId)
            || await _db.Reviews.AnyAsync(r => r.ProductId == productId);

        if (referenced)
        {
            product.IsAvailable = false;
            product.UpdatedAt = DateTime.UtcNow;
            _db.Products.Update(product);
        }
        else
        {
            var oldLocalImageUrls = await _db.ProductImages
                .Where(i => i.ProductId == productId)
                .Select(i => i.ImageUrl)
                .ToListAsync();

            _db.ProductImages.RemoveRange(_db.ProductImages.Where(i => i.ProductId == productId));
            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            foreach (var url in oldLocalImageUrls)
                _imageStore.DeleteIfLocal(url);
            return;
        }

        await _db.SaveChangesAsync();
    }

    private static Season GetCurrentSeason()
    {
        var month = DateTime.UtcNow.Month;
        return month switch
        {
            3 or 4 or 5 => Season.Spring,
            6 or 7 or 8 => Season.Summer,
            9 or 10 or 11 => Season.Autumn,
            _ => Season.Winter
        };
    }

    private static string GenerateSlug(string name, int farmerId)
    {
        var slug = name.ToLower().Replace(" ", "-").Replace("'", "");
        return $"{slug}-{farmerId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    }
}
