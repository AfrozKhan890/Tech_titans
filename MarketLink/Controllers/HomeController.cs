using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarketLink.Repositories;
using MarketLink.Services;

namespace MarketLink.Controllers;

public class HomeController : Controller
{
    private readonly MarketLink.Services.IProductService _productService;
    private readonly IUnitOfWork _unitOfWork;

    public HomeController(MarketLink.Services.IProductService productService, IUnitOfWork unitOfWork)
    {
        _productService = productService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _productService.GetFeaturedAsync();


        var categories = await _unitOfWork.Repository<MarketLink.Models.Category>()
            .Query()
            .Include(c => c.Products)
            .Include(c => c.Children).ThenInclude(ch => ch.Products)
            .Where(c => c.ParentId == null && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        var markets = await _unitOfWork.Repository<MarketLink.Models.Market>().Query().Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync();

        var favoriteProductIds = new HashSet<int>();
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Customer"))
        {
            var customerId = await ActorResolver.ResolveCustomerIdAsync(User, _unitOfWork, HttpContext);
            if (customerId.HasValue)
            {
                favoriteProductIds = (await _unitOfWork.Repository<MarketLink.Models.Favorite>().Query()
                    .Where(f => f.CustomerId == customerId.Value && f.ProductId.HasValue)
                    .Select(f => f.ProductId!.Value)
                    .ToListAsync())
                    .ToHashSet();
            }
        }

        var now = DateTime.UtcNow;
        var announcements = await _unitOfWork.Repository<MarketLink.Models.Announcement>().Query().AsNoTracking()
            .Where(a => a.IsActive && (!a.StartsAt.HasValue || a.StartsAt <= now) && (!a.EndsAt.HasValue || a.EndsAt >= now))
            .OrderByDescending(a => a.CreatedAt).Take(5).ToListAsync();
        ViewBag.Categories = categories;
        ViewBag.Markets = markets;
        ViewBag.Announcements = announcements;
        ViewBag.FavoriteProductIds = favoriteProductIds;
        return View(products.Take(8).ToList());
    }

    public IActionResult About() => View();

    public async Task<IActionResult> Contact()
    {
        var market = await _unitOfWork.Repository<MarketLink.Models.Market>().Query().AsNoTracking()
            .Where(m => m.IsActive).OrderBy(m => m.Name).FirstOrDefaultAsync();
        ViewBag.ContactMarket = market;
        return View();
    }

    public IActionResult FAQ() => View();

    public IActionResult Privacy() => View();

    public IActionResult Terms() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new MarketLink.Models.ErrorViewModel
    {
        RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier
    });



    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult HttpStatus(int code)
    {
        ViewData["StatusCode"] = code;
        return View(new MarketLink.Models.ErrorViewModel
        {
            RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
