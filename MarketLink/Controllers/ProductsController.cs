using Microsoft.AspNetCore.Mvc;
using MarketLink.Repositories;
using MarketLink.Services;
using MarketLink.Models;
using MarketLink.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarketLink.Controllers;

public class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly IUnitOfWork _unitOfWork;

    public ProductsController(IProductService productService, IUnitOfWork unitOfWork)
    {
        _productService = productService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index(
        string? categorySlug,
        string? q,
        bool? isOrganic,
        bool? seasonal,
        decimal? minPrice,
        decimal? maxPrice,
        string? sortBy,
        int? marketId,
        string? marketDay,
        string? location,
        double? latitude,
        double? longitude,
        double? radiusKm,
        int page = 1)
    {
        var productQuery = _unitOfWork.Repository<Product>().Query()
            .Include(p => p.Farmer).ThenInclude(f => f.User)
            .Include(p => p.Farmer).ThenInclude(f => f.FarmerMarkets).ThenInclude(fm => fm.Market)
            .Include(p => p.Farmer).ThenInclude(f => f.PickupSlots)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Where(p => p.IsAvailable && p.StockQuantityKg > 0 && p.Farmer.Status == FarmerStatus.Approved &&
                        p.Farmer.FarmerMarkets.Any(fm => fm.IsActive && fm.Market.IsActive));


        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            productQuery = productQuery.Where(p => p.Category.Slug == categorySlug || (p.Category.Parent != null && p.Category.Parent.Slug == categorySlug));
        }


        if (!string.IsNullOrWhiteSpace(q))
        {
            var searchTerm = q.Trim();
            productQuery = productQuery.Where(p =>
                p.Name.Contains(searchTerm) ||
                (p.Description != null && p.Description.Contains(searchTerm)) ||
                (p.Tags != null && p.Tags.Contains(searchTerm)) ||
                p.Category.Name.Contains(searchTerm) ||
                p.Farmer.FarmName.Contains(searchTerm));
        }


        if (isOrganic == true)
        {
            productQuery = productQuery.Where(p => p.IsOrganic);
        }


        if (seasonal == true)
        {
            var currentSeason = GetCurrentSeason();
            productQuery = productQuery.Where(p => p.Season == currentSeason || p.Season == Season.AllYear);
        }


        if (minPrice.HasValue && minPrice.Value > 0)
        {
            productQuery = productQuery.Where(p => p.PricePerKg >= minPrice.Value);
        }

        if (maxPrice.HasValue && maxPrice.Value > 0)
        {
            productQuery = productQuery.Where(p => p.PricePerKg <= maxPrice.Value);
        }


        if (marketId.HasValue)
        {
            productQuery = productQuery.Where(p => p.Farmer.FarmerMarkets.Any(fm =>
                fm.MarketId == marketId.Value && fm.IsActive && fm.Market.IsActive));
        }



        if (!string.IsNullOrWhiteSpace(marketDay) && Enum.TryParse<DayOfWeek>(marketDay, true, out var selectedDay))
        {
            var dayName = selectedDay.ToString();
            productQuery = productQuery.Where(p => p.Farmer.FarmerMarkets.Any(fm => fm.IsActive && fm.Market.IsActive &&
                ((fm.Market.OpenDays != null && fm.Market.OpenDays.Contains(dayName)) ||
                 fm.Farmer.PickupSlots.Any(ps => ps.IsActive && ps.MarketId == fm.MarketId && ps.DayOfWeek == selectedDay))));
        }


        if (!string.IsNullOrWhiteSpace(location))
        {
            var locationTerm = location.Trim();
            productQuery = productQuery.Where(p =>
                (p.Farmer.City != null && p.Farmer.City.Contains(locationTerm)) ||
                (p.Farmer.State != null && p.Farmer.State.Contains(locationTerm)) ||
                p.Farmer.FarmerMarkets.Any(fm => fm.IsActive && fm.Market.IsActive &&
                    (fm.Market.City.Contains(locationTerm) || fm.Market.State.Contains(locationTerm))));
        }

        var radius = radiusKm.GetValueOrDefault(50);
        if (latitude.HasValue && longitude.HasValue)
        {
            var latDelta = radius / 111.32;
            var lonDelta = radius / (111.32 * Math.Max(0.2, Math.Cos(latitude.Value * Math.PI / 180d)));
            var minLat = latitude.Value - latDelta; var maxLat = latitude.Value + latDelta;
            var minLon = longitude.Value - lonDelta; var maxLon = longitude.Value + lonDelta;
            productQuery = productQuery.Where(p =>
                p.Farmer.FarmerMarkets.Any(fm => fm.IsActive && fm.Market.IsActive && fm.Market.Latitude >= minLat && fm.Market.Latitude <= maxLat && fm.Market.Longitude >= minLon && fm.Market.Longitude <= maxLon) ||
                (p.Farmer.Latitude.HasValue && p.Farmer.Longitude.HasValue && p.Farmer.Latitude.Value >= minLat && p.Farmer.Latitude.Value <= maxLat && p.Farmer.Longitude.Value >= minLon && p.Farmer.Longitude.Value <= maxLon));
        }


        productQuery = sortBy switch
        {
            "price_asc" => productQuery.OrderBy(p => p.PricePerKg),
            "price_desc" => productQuery.OrderByDescending(p => p.PricePerKg),
            "name" => productQuery.OrderBy(p => p.Name),
            "name_desc" => productQuery.OrderByDescending(p => p.Name),
            "bestseller" => productQuery.OrderByDescending(p => p.IsBestSeller).ThenByDescending(p => p.CreatedAt),
            "newest" => productQuery.OrderByDescending(p => p.CreatedAt),
            _ => productQuery.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.Id)
        };

        const int pageSize = 24;
        page = Math.Max(1, page);
        int totalCount;
        List<Product> products;
        if (latitude.HasValue && longitude.HasValue)
        {


            var candidates = await productQuery.AsNoTracking().ToListAsync();
            candidates = candidates.Where(p => p.Farmer.FarmerMarkets.Any(fm => fm.IsActive && fm.Market.IsActive && DistanceKm(latitude.Value, longitude.Value, fm.Market.Latitude, fm.Market.Longitude) <= radius) ||
                (p.Farmer.Latitude.HasValue && p.Farmer.Longitude.HasValue && DistanceKm(latitude.Value, longitude.Value, p.Farmer.Latitude.Value, p.Farmer.Longitude.Value) <= radius)).ToList();
            totalCount = candidates.Count;
            products = candidates.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }
        else
        {
            totalCount = await productQuery.AsNoTracking().CountAsync();
            products = await productQuery.AsNoTracking().Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        }


        var categories = await _unitOfWork.Repository<Category>().GetAllAsync();
        ViewBag.Markets = await _unitOfWork.Repository<Market>().Query().Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync();
        ViewBag.Categories = categories.OrderBy(c => c.Name).ToList();


        var favoriteProductIds = new HashSet<int>();
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var customers = await _unitOfWork.Repository<Customer>().FindAsync(c => c.UserId == userId);
                var customer = customers.FirstOrDefault();
                if (customer != null)
                {
                    var favs = await _unitOfWork.Repository<Favorite>().FindAsync(f => f.CustomerId == customer.Id && f.ProductId.HasValue);
                    favoriteProductIds = favs.Select(f => f.ProductId!.Value).ToHashSet();
                }
            }
        }

        ViewBag.FavoriteProductIds = favoriteProductIds;
        ViewBag.CurrentCategory = categorySlug;
        ViewBag.Query = q;
        ViewBag.IsOrganic = isOrganic;
        ViewBag.Seasonal = seasonal;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.SortBy = sortBy;
        ViewBag.MarketId = marketId;
        ViewBag.MarketDay = marketDay;
        ViewBag.Location = location;
        ViewBag.Latitude = latitude;
        ViewBag.Longitude = longitude;
        ViewBag.RadiusKm = radiusKm.GetValueOrDefault(50);
        ViewBag.TotalCount = totalCount;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return View(products);
    }

    public async Task<IActionResult> Details(string slug)
    {
        if (string.IsNullOrEmpty(slug)) return NotFound();
        var product = await _productService.GetBySlugAsync(slug);
        if (product == null) return NotFound();
        if (product.Farmer.Status != FarmerStatus.Approved || !product.Farmer.FarmerMarkets.Any(fm => fm.IsActive && fm.Market.IsActive)) return NotFound();

        bool isFav = false;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var customers = await _unitOfWork.Repository<Customer>().FindAsync(c => c.UserId == userId);
                var customer = customers.FirstOrDefault();
                if (customer != null)
                {
                    var favs = await _unitOfWork.Repository<Favorite>().FindAsync(f => f.CustomerId == customer.Id && f.ProductId == product.Id);
                    isFav = favs.Any();
                }
            }
        }
        ViewBag.IsFavorite = isFav;

        var canReview = false;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var customer = await _unitOfWork.Repository<Customer>().FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer != null)
                {
                    canReview = await _unitOfWork.Repository<Order>().Query()
                        .Where(o => o.CustomerId == customer.Id && o.Status == OrderStatus.Completed)
                        .AnyAsync(o => o.Items.Any(i => i.ProductId == product.Id));
                }
            }
        }
        ViewBag.CanReview = canReview;
        ViewBag.Reviews = product.Reviews.Where(r => r.IsApproved).OrderByDescending(r => r.CreatedAt).ToList();
        ViewBag.AverageRating = product.Reviews.Where(r => r.IsApproved).Select(r => (double?)r.Rating).Average() ?? 0;

        return View(product);
    }

    [HttpGet]
    public async Task<IActionResult> Autocomplete(string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(new object[] { });

        var searchTerm = q.Trim();
        var results = await _unitOfWork.Repository<Product>().Query()
            .Include(p => p.Category)
            .Where(p => p.IsAvailable && p.StockQuantityKg > 0
                        && p.Farmer.Status == FarmerStatus.Approved
                        && p.Farmer.FarmerMarkets.Any(fm => fm.IsActive && fm.Market.IsActive)
                        && (p.Name.Contains(searchTerm) || (p.Description != null && p.Description.Contains(searchTerm))))
            .Take(6)
            .Select(p => new
            {
                p.Name,
                p.Slug,
                p.PricePerKg,
                p.ImageUrl,
                CategoryName = p.Category != null ? p.Category.Name : "Produce",
                FarmerName = p.Farmer.FarmName
            })
            .ToListAsync();

        return Json(results);
    }

    private static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0088;
        var dLat = (lat2 - lat1) * Math.PI / 180d;
        var dLon = (lon2 - lon1) * Math.PI / 180d;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1 * Math.PI / 180d) * Math.Cos(lat2 * Math.PI / 180d)
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return earthRadiusKm * 2 * Math.Asin(Math.Sqrt(a));
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
}
