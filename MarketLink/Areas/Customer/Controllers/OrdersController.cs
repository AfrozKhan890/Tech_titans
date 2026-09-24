using System.Security.Cryptography;
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
    public class OrdersController : CustomerControllerBase
    {
        public OrdersController(MarketLinkDbContext db, ICustomerIdentityService identity) : base(db, identity) { }

        public async Task<IActionResult> Index() => await History(null, null);

        public async Task<IActionResult> History(string? statusFilter, string? q)
        {
            var customer = await CurrentCustomerAsync();
            if (customer == null) return Challenge(CustomerAuthDefaults.AuthenticationScheme);

            var query = Db.Orders
                .Include(o => o.Farmer)
                .Include(o => o.Market)
                .Include(o => o.OrderItems).ThenInclude(i => i.Product)
                .Where(o => o.CustomerId == customer.CustomerId);

            if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<OrderStatus>(statusFilter, true, out var status))
                query = query.Where(o => o.OrderStatus == status);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(o => (o.OrderNumber != null && o.OrderNumber.Contains(term)) ||
                                         (o.Farmer != null && o.Farmer.StallName.Contains(term)) ||
                                         o.OrderItems.Any(i => i.Product != null && i.Product.Name.Contains(term)));
            }

            ViewBag.StatusFilter = statusFilter;
            ViewBag.Query = q;
            return View(await query.OrderByDescending(o => o.OrderDate).ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var customer = await CurrentCustomerAsync();
            if (customer == null) return Challenge(CustomerAuthDefaults.AuthenticationScheme);

            var order = await Db.Orders
                .Include(o => o.Farmer).Include(o => o.Market)
                .Include(o => o.OrderItems).ThenInclude(i => i.Product).ThenInclude(p => p!.Farmer)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.CustomerId == customer.CustomerId);

            return order == null ? NotFound() : View(order);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reorder(int id)
        {
            var customer = await CurrentCustomerAsync();
            if (customer == null) return Challenge(CustomerAuthDefaults.AuthenticationScheme);

            var order = await Db.Orders.Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.CustomerId == customer.CustomerId);
            if (order == null) { TempData["Error"] = "Order not found."; return RedirectToAction(nameof(History)); }

            var added = 0;
            foreach (var oldItem in order.OrderItems)
            {
                var product = await Db.Products.FirstOrDefaultAsync(p => p.ProductId == oldItem.ProductId && p.Status == ProductStatus.Active);
                if (product == null || product.StockQuantity <= 0) continue;
                var item = await Db.CartItems.FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId && c.ProductId == oldItem.ProductId);
                var qty = Math.Min(oldItem.Quantity, product.StockQuantity);
                if (item == null) Db.CartItems.Add(new CartItem { CustomerId = customer.CustomerId, ProductId = product.ProductId, QuantityKg = qty });
                else item.QuantityKg = Math.Min(item.QuantityKg + qty, product.StockQuantity);
                added++;
            }
            await Db.SaveChangesAsync();
            TempData[added > 0 ? "Success" : "Error"] = added > 0 ? $"Added {added} item(s) to your basket." : "The products are no longer available.";
            return RedirectToAction("Index", "Cart");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int marketId, DateTime pickupTime)
        {
            var customer = await CurrentCustomerAsync();
            if (customer == null) return Challenge(CustomerAuthDefaults.AuthenticationScheme);

            var cart = await Db.CartItems.Include(c => c.Product)
                .Where(c => c.CustomerId == customer.CustomerId).ToListAsync();
            if (cart.Count == 0) { TempData["Error"] = "Your cart is empty."; return RedirectToAction("Index", "Cart"); }

            var invalid = cart.FirstOrDefault(c => c.Product == null || c.Product.Status != ProductStatus.Active || c.QuantityKg <= 0 || c.QuantityKg > c.Product.StockQuantity);
            if (invalid != null) { TempData["Error"] = "One or more cart items are unavailable or exceed stock."; return RedirectToAction("Index", "Cart"); }

            var groups = cart.GroupBy(c => c.Product!.FarmerId).ToList();
            Order? firstOrder = null;

            foreach (var group in groups)
            {
                var order = new Order
                {
                    CustomerId = customer.CustomerId,
                    FarmerId = group.Key,
                    MarketId = marketId > 0 ? marketId : null,
                    OrderNumber = $"ML-{DateTime.UtcNow:yyyyMMddHHmmss}-{RandomNumberGenerator.GetInt32(1000, 9999)}",
                    OrderStatus = OrderStatus.Placed,
                    OrderDate = DateTime.UtcNow,
                    PickupDate = pickupTime == default ? null : DateOnly.FromDateTime(pickupTime),
                    PickupTime = pickupTime == default ? null : pickupTime.TimeOfDay,
                    PaymentMethod = "Cash on Pickup"
                };

                foreach (var cartItem in group)
                {
                    var product = cartItem.Product!;
                    var line = product.Price * cartItem.QuantityKg;
                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = product.ProductId,
                        Quantity = cartItem.QuantityKg,
                        UnitPrice = product.Price,
                        LineTotal = line,
                        ProductNameSnapshot = product.Name
                    });
                    order.TotalAmount += line;
                    product.StockQuantity -= cartItem.QuantityKg;
                    if (product.StockQuantity <= 0) product.Status = ProductStatus.SoldOut;
                }

                Db.Orders.Add(order);
                Db.Notifications.Add(new Notification
                {
                    UserId = customer.CustomerId.ToString(),
                    Title = "Order placed",
                    Message = $"Your order {order.OrderNumber} was placed successfully.",
                    Link = $"/Customer/Orders/Details/{order.OrderId}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
                firstOrder ??= order;
            }

            Db.CartItems.RemoveRange(cart);
            await Db.SaveChangesAsync();

            if (firstOrder == null) { TempData["Error"] = "Unable to create order."; return RedirectToAction("Index", "Cart"); }
            TempData["Success"] = $"Order #{firstOrder.OrderNumber} placed successfully.";
            return RedirectToAction(nameof(Details), new { id = firstOrder.OrderId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var customer = await CurrentCustomerAsync();
            if (customer == null) return Challenge(CustomerAuthDefaults.AuthenticationScheme);

            var order = await Db.Orders.Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.CustomerId == customer.CustomerId);
            if (order == null) return NotFound();
            if (order.OrderStatus is OrderStatus.Completed or OrderStatus.Cancelled)
            {
                TempData["Error"] = "This order cannot be cancelled.";
                return RedirectToAction(nameof(Details), new { id });
            }

            foreach (var item in order.OrderItems)
            {
                var product = await Db.Products.FindAsync(item.ProductId);
                if (product != null) { product.StockQuantity += item.Quantity; if (product.Status == ProductStatus.SoldOut) product.Status = ProductStatus.Active; }
            }
            order.OrderStatus = OrderStatus.Cancelled;
            order.CancellationReason = "Customer request";
            order.CancelledAt = DateTime.UtcNow;
            Db.Notifications.Add(new Notification
            {
                UserId = customer.CustomerId.ToString(),
                Title = "Order cancelled",
                Message = $"Your order {order.OrderNumber ?? order.OrderId.ToString()} was cancelled.",
                Link = $"/Customer/Orders/Details/{order.OrderId}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await Db.SaveChangesAsync();
            TempData["Success"] = "Order cancelled successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
