using System.Security.Claims;
using MarketLink.Data;
using MarketLink.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Controllers
{
    public class ProductsController : Controller
    {
        private readonly MarketLinkDbContext _context;
        public ProductsController(MarketLinkDbContext context) => _context = context;

        public async Task<IActionResult> Index(
            string? categorySlug, string? q, bool? isOrganic, bool? seasonal,
            decimal? minPrice, decimal? maxPrice, string? sortBy, int page = 1)
        {
            var query = _context.Products
                .Include(p => p.Farmer)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Where(p => p.Status == ProductStatus.Active &&
                            p.Farmer != null && p.Farmer.Status == FarmerStatus.Approved);

            if (!string.IsNullOrWhiteSpace(categorySlug))
                query = query.Where(p => p.Category != null && p.Category.Slug == categorySlug);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(p =>
                    p.Name.Contains(term) ||
                    (p.Description != null && p.Description.Contains(term)) ||
                    (p.Tags != null && p.Tags.Contains(term)) ||
                    (p.Category != null && p.Category.Name.Contains(term)) ||
                    (p.Farmer != null && p.Farmer.StallName.Contains(term)));
            }

            if (isOrganic == true) query = query.Where(p => p.IsOrganic);
            if (seasonal == true)
            {
                var season = GetCurrentSeason();
                query = query.Where(p => p.Season == season || p.Season == Season.AllYear);
            }
            if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);

            query = sortBy switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                "bestseller" => query.OrderByDescending(p => p.IsBestSeller).ThenByDescending(p => p.CreatedAt),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var products = await query.Skip(Math.Max(0, page - 1) * 24).Take(24).ToListAsync();
            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            ViewBag.FavoriteProductIds = await GetFavoriteProductIdsAsync();
            ViewBag.CurrentCategory = categorySlug;
            ViewBag.Query = q;
            ViewBag.IsOrganic = isOrganic;
            ViewBag.Seasonal = seasonal;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.SortBy = sortBy;
            ViewBag.TotalCount = totalCount;
            return View(products);
        }

        public async Task<IActionResult> Details(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return NotFound();
            var product = await _context.Products
                .Include(p => p.Farmer)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Reviews).ThenInclude(r => r.Customer)
                .FirstOrDefaultAsync(p => p.Slug == slug);

            if (product == null) return NotFound();
            ViewBag.IsFavorite = (await GetFavoriteProductIdsAsync()).Contains(product.ProductId);
            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> Autocomplete(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return Json(Array.Empty<object>());
            var term = q.Trim();
            var results = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Status == ProductStatus.Active &&
                            (p.Name.Contains(term) || (p.Description != null && p.Description.Contains(term))))
                .Take(6)
                .Select(p => new
                {
                    p.Name,
                    Slug = p.Slug ?? p.ProductId.ToString(),
                    PricePerKg = p.Price,
                    p.ImageUrl,
                    CategoryName = p.Category != null ? p.Category.Name : "Produce"
                }).ToListAsync();
            return Json(results);
        }

        private async Task<HashSet<int>> GetFavoriteProductIdsAsync()
        {
            if (!User.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "Customer")) return [];
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return [];
            return (await _context.Favorites.Where(f => f.CustomerId == customerId && f.ProductId.HasValue)
                .Select(f => f.ProductId!.Value).ToListAsync()).ToHashSet();
        }

        private static Season GetCurrentSeason() => DateTime.UtcNow.Month switch
        {
            3 or 4 or 5 => Season.Spring,
            6 or 7 or 8 => Season.Summer,
            9 or 10 or 11 => Season.Autumn,
            _ => Season.Winter
        };
    }
}
