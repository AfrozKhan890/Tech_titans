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
    public class ReviewsController : CustomerControllerBase
    {
        public ReviewsController(MarketLinkDbContext db, ICustomerIdentityService identity) : base(db, identity) { }

        public async Task<IActionResult> Index()
        {
            var customer = await CurrentCustomerAsync();
            if (customer == null) return Challenge(CustomerAuthDefaults.AuthenticationScheme);
            var reviews = await Db.Reviews.Include(r => r.Product).Include(r => r.Farmer)
                .Where(r => r.CustomerId == customer.CustomerId && !r.IsRemoved)
                .OrderByDescending(r => r.ReviewDate).ToListAsync();
            return View(reviews);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int orderId, int productId)
        {
            var customer = await CurrentCustomerAsync();
            if (customer == null) return Challenge(CustomerAuthDefaults.AuthenticationScheme);

            var eligible = await Db.Orders
                .Include(o => o.OrderItems)
                .AnyAsync(o => o.OrderId == orderId && o.CustomerId == customer.CustomerId &&
                               o.OrderStatus == OrderStatus.Completed &&
                               o.OrderItems.Any(i => i.ProductId == productId));
            if (!eligible) return NotFound();

            var product = await Db.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null) return NotFound();

            ViewBag.Product = product;
            ViewBag.OrderId = orderId;
            return View(new CustomerReviewCreateViewModel { OrderId = orderId, ProductId = productId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerReviewCreateViewModel model)
        {
            var customer = await CurrentCustomerAsync();
            if (customer == null) return Challenge(CustomerAuthDefaults.AuthenticationScheme);
            if (!ModelState.IsValid) return View(model);

            var eligible = await Db.Orders.Include(o => o.OrderItems)
                .AnyAsync(o => o.OrderId == model.OrderId && o.CustomerId == customer.CustomerId &&
                               o.OrderStatus == OrderStatus.Completed &&
                               o.OrderItems.Any(i => i.ProductId == model.ProductId));
            if (!eligible) return NotFound();

            if (await Db.Reviews.AnyAsync(r => r.CustomerId == customer.CustomerId && r.ProductId == model.ProductId))
            {
                TempData["Error"] = "You have already reviewed this product for this order.";
                return RedirectToAction(nameof(Index));
            }

            var product = await Db.Products.FirstAsync(p => p.ProductId == model.ProductId);
            Db.Reviews.Add(new Review
            {
                CustomerId = customer.CustomerId,
                ProductId = model.ProductId,
                FarmerId = product.FarmerId,
                Rating = model.Rating,
                Title = model.Title?.Trim(),
                Comment = model.Comment?.Trim(),
                IsApproved = true,
                IsRemoved = false,
                ReviewDate = DateTime.UtcNow
            });
            await Db.SaveChangesAsync();
            TempData["Success"] = "Thank you for your review.";
            return RedirectToAction(nameof(Index));
        }
    }
}
