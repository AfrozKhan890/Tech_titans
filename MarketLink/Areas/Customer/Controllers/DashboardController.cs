using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MarketLink.Repositories;
using MarketLink.Services;
using MarketLink.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
public class DashboardController : MarketLink.Services.CustomerAreaController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductService _productService;
    private readonly IOrderService _orderService;

    public DashboardController(IUnitOfWork unitOfWork, IProductService productService, IOrderService orderService)
    {
        _unitOfWork = unitOfWork;
        _productService = productService;
        _orderService = orderService;
    }

    public async Task<IActionResult> Index()
    {
        var custId = CustomerId;
        var me = await _unitOfWork.Repository<MarketLink.Models.Customer>().Query()
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == custId);

        var custName = $"{me?.User?.FirstName} {me?.User?.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(custName)) custName = User.Identity?.Name ?? "Customer";
        var city = me?.DefaultCity;


        var products = await _unitOfWork.Repository<Product>().Query()
            .Include(p => p.Category)
            .Include(p => p.Farmer)
            .Include(p => p.Images)
            .Where(p => p.IsAvailable && p.Farmer.Status == MarketLink.Models.Enums.FarmerStatus.Approved)
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.Id)
            .Take(8)
            .ToListAsync();


        var favorites = await _unitOfWork.Repository<Favorite>().Query()
            .Include(f => f.Product).ThenInclude(p => p!.Category)
            .Include(f => f.Product).ThenInclude(p => p!.Farmer)
            .Include(f => f.Farmer)
            .Where(f => f.CustomerId == custId)
            .OrderByDescending(f => f.CreatedAt)
            .Take(6)
            .ToListAsync();

        var favProductIds = favorites.Where(f => f.ProductId.HasValue).Select(f => f.ProductId!.Value).ToHashSet();


        var recentOrders = await _unitOfWork.Repository<Order>().Query()
            .Include(o => o.Farmer)
            .Include(o => o.Items)
            .Where(o => o.CustomerId == custId)
            .OrderByDescending(o => o.OrderedAt)
            .Take(5)
            .ToListAsync();

        var allOrders = await _unitOfWork.Repository<Order>().FindAsync(o => o.CustomerId == custId);
        var addresses = await _unitOfWork.Repository<CustomerAddress>().FindAsync(a => a.CustomerId == custId);
        var categories = await _unitOfWork.Repository<Category>().GetAllAsync();

        var totalSpent = allOrders.Where(o => o.Status != MarketLink.Models.Enums.OrderStatus.Cancelled).Sum(o => o.TotalAmount);

        var model = new DashboardViewModel
        {
            CustomerName = custName,
            DefaultCity = city,
            TotalOrdersCount = allOrders.Count(),
            TotalFavoritesCount = favorites.Count,
            TotalAddressesCount = addresses.Count(),
            TotalSpent = totalSpent,
            Products = products,
            Categories = categories.Take(6),
            Favorites = favorites,
            RecentOrders = recentOrders,
            FavoriteProductIds = favProductIds
        };

        return View(model);
    }

    public IActionResult Settings() => RedirectToAction("Index", "Profile");
}
