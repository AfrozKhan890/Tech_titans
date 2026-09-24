using MarketLink.Areas.Customer;
using MarketLink.Areas.Customer.Services;
using MarketLink.Data;
using MarketLink.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(AuthenticationSchemes = CustomerAuthDefaults.AuthenticationScheme, Policy = CustomerAuthDefaults.CustomerPolicy)]
    public class FavoritesController : CustomerControllerBase
    {
        public FavoritesController(MarketLinkDbContext db, ICustomerIdentityService identity) : base(db, identity) { }

        public async Task<IActionResult> Index(string? tab = "all")
        {
            var customer = await CurrentCustomerAsync();
            if (customer == null) return Challenge(CustomerAuthDefaults.AuthenticationScheme);
            ViewData["ActiveTab"] = tab?.ToLowerInvariant() ?? "all";

            var favorites = await Db.Favorites
                .Include(f => f.Product).ThenInclude(p => p!.Category)
                .Include(f => f.Product).ThenInclude(p => p!.Farmer)
                .Include(f => f.Product).ThenInclude(p => p!.Images)
                .Include(f => f.Farmer)
                .Where(f => f.CustomerId == customer.CustomerId)
                .OrderByDescending(f => f.CreatedAt).ToListAsync();

            return View(favorites);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(string type, int id)
        {
            var customer = await CurrentCustomerAsync();
            if (customer == null) return Unauthorized();

            type = type?.Trim().ToLowerInvariant() ?? string.Empty;
            Favorite? existing = type switch
            {
                "product" => await Db.Favorites.FirstOrDefaultAsync(f => f.CustomerId == customer.CustomerId && f.ProductId == id),
                "farmer" => await Db.Favorites.FirstOrDefaultAsync(f => f.CustomerId == customer.CustomerId && f.FarmerId == id),
                _ => null
            };
            if (type is not ("product" or "farmer") || id <= 0)
                return Json(new { success = false, message = "Invalid favorite target." });

            if (existing != null)
            {
                Db.Favorites.Remove(existing);
                await Db.SaveChangesAsync();
                return Json(new { success = true, isFavorite = false });
            }

            if (type == "product" && !await Db.Products.AnyAsync(p => p.ProductId == id)) return Json(new { success = false, message = "Product not found." });
            if (type == "farmer" && !await Db.Farmers.AnyAsync(f => f.FarmerId == id)) return Json(new { success = false, message = "Farmer not found." });

            Db.Favorites.Add(new Favorite { CustomerId = customer.CustomerId, ProductId = type == "product" ? id : null, FarmerId = type == "farmer" ? id : null });
            await Db.SaveChangesAsync();
            return Json(new { success = true, isFavorite = true });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var customer = await CurrentCustomerAsync();
            if (customer != null)
            {
                var favorite = await Db.Favorites.FirstOrDefaultAsync(f => f.Id == id && f.CustomerId == customer.CustomerId);
                if (favorite != null) { Db.Favorites.Remove(favorite); await Db.SaveChangesAsync(); }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
