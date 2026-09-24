using System.Text.Json;
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
    public class CartController : CustomerControllerBase
    {
        public CartController(MarketLinkDbContext db, ICustomerIdentityService identity) : base(db, identity) { }

        private async Task<int?> CustomerIdAsync() => (await CurrentCustomerAsync())?.CustomerId;

        public async Task<IActionResult> Index()
        {
            var id = await CustomerIdAsync();
            if (id == null) return Challenge(CustomerAuthDefaults.AuthenticationScheme);
            var items = await Db.CartItems.Include(c => c.Product).ThenInclude(p => p!.Farmer)
                .Where(c => c.CustomerId == id.Value).OrderByDescending(c => c.AddedAt).ToListAsync();
            ViewBag.Markets = await Db.Markets.Where(m => m.Status == MarketStatus.Active).OrderBy(m => m.MarketName).ToListAsync();
            return View(items);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem([FromBody] AddToCartRequest req)
        {
            var customerId = await CustomerIdAsync();
            if (customerId == null) return Unauthorized();
            if (req.ProductId <= 0 || req.QuantityKg <= 0) return Json(new { success = false, message = "Invalid quantity." });

            var product = await Db.Products.FirstOrDefaultAsync(p => p.ProductId == req.ProductId && p.Status == ProductStatus.Active);
            if (product == null) return Json(new { success = false, message = "Product is unavailable." });
            if (req.QuantityKg > product.StockQuantity) return Json(new { success = false, message = "Requested quantity exceeds available stock." });

            var item = await Db.CartItems.FirstOrDefaultAsync(c => c.CustomerId == customerId && c.ProductId == req.ProductId);
            if (item == null)
                Db.CartItems.Add(new CartItem { CustomerId = customerId.Value, ProductId = req.ProductId, QuantityKg = req.QuantityKg });
            else
            {
                item.QuantityKg += req.QuantityKg;
                if (item.QuantityKg > product.StockQuantity) return Json(new { success = false, message = "Requested quantity exceeds available stock." });
            }

            await Db.SaveChangesAsync();
            var count = await Db.CartItems.Where(c => c.CustomerId == customerId).SumAsync(c => (int?)c.QuantityKg) ?? 0;
            return Json(new { success = true, cartCount = count });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem([FromBody] RemoveFromCartRequest req)
        {
            var customerId = await CustomerIdAsync();
            if (customerId == null) return Unauthorized();
            var item = await Db.CartItems.FirstOrDefaultAsync(c => c.CustomerId == customerId && c.ProductId == req.ProductId);
            if (item != null) { Db.CartItems.Remove(item); await Db.SaveChangesAsync(); }
            return Json(new { success = true });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            var id = await CustomerIdAsync();
            if (id != null)
            {
                var items = Db.CartItems.Where(c => c.CustomerId == id.Value);
                Db.CartItems.RemoveRange(items);
                await Db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }

    public sealed class AddToCartRequest { public int ProductId { get; set; } public int QuantityKg { get; set; } }
    public sealed class RemoveFromCartRequest { public int ProductId { get; set; } }
}
