using MarketLink.Areas.Customer;
using MarketLink.Areas.Customer.Services;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(AuthenticationSchemes = CustomerAuthDefaults.AuthenticationScheme, Policy = CustomerAuthDefaults.CustomerPolicy)]
    public class DashboardController : CustomerControllerBase
    {
        public DashboardController(MarketLinkDbContext db, ICustomerIdentityService identity) : base(db, identity) { }

        public async Task<IActionResult> Index()
        {
            var customer = await CurrentCustomerAsync();
            if (customer == null) return Challenge(CustomerAuthDefaults.AuthenticationScheme);

            var products = await Db.Products
                .Include(p => p.Category).Include(p => p.Farmer).Include(p => p.Images)
                .Where(p => p.Status == ProductStatus.Active && p.Farmer != null && p.Farmer.Status == FarmerStatus.Approved)
                .OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAt).Take(8).ToListAsync();

            var favorites = await Db.Favorites
                .Include(f => f.Product).ThenInclude(p => p!.Category)
                .Include(f => f.Product).ThenInclude(p => p!.Farmer)
                .Include(f => f.Farmer)
                .Where(f => f.CustomerId == customer.CustomerId)
                .OrderByDescending(f => f.CreatedAt).Take(6).ToListAsync();

            var recentOrders = await Db.Orders
                .Include(o => o.Farmer).Include(o => o.OrderItems).ThenInclude(i => i.Product)
                .Where(o => o.CustomerId == customer.CustomerId)
                .OrderByDescending(o => o.OrderDate).Take(5).ToListAsync();

            var totalOrders = await Db.Orders.CountAsync(o => o.CustomerId == customer.CustomerId);
            var totalSpent = await Db.Orders.Where(o => o.CustomerId == customer.CustomerId && o.OrderStatus != OrderStatus.Cancelled)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            var totalAddresses = await Db.CustomerAddresses.CountAsync(a => a.CustomerId == customer.CustomerId);
            var totalFavorites = await Db.Favorites.CountAsync(f => f.CustomerId == customer.CustomerId);
            var categories = await Db.Categories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ThenBy(c => c.Name).Take(6).ToListAsync();

            var model = new DashboardViewModel
            {
                CustomerName = customer.FullName,
                DefaultCity = customer.DefaultCity,
                TotalOrdersCount = totalOrders,
                TotalFavoritesCount = totalFavorites,
                TotalAddressesCount = totalAddresses,
                TotalSpent = totalSpent,
                Products = products,
                Categories = categories,
                Favorites = favorites,
                RecentOrders = recentOrders,
                FavoriteProductIds = favorites.Where(f => f.ProductId.HasValue).Select(f => f.ProductId!.Value).ToHashSet()
            };
            return View(model);
        }

        public IActionResult Settings() => RedirectToAction("Index", "Profile");
    }
}
