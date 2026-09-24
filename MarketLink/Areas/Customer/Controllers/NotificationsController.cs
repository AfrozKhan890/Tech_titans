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
    public class NotificationsController : CustomerControllerBase
    {
        public NotificationsController(MarketLinkDbContext db, ICustomerIdentityService identity) : base(db, identity) { }

        public async Task<IActionResult> Index()
        {
            var customer = await CurrentCustomerAsync();
            if (customer == null) return Challenge(CustomerAuthDefaults.AuthenticationScheme);

            var notifications = await Db.Notifications
                .Where(n => n.UserId == customer.CustomerId.ToString())
                .OrderByDescending(n => n.CreatedAt).ToListAsync();
            return View(notifications);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var customer = await CurrentCustomerAsync();
            if (customer != null)
            {
                var notification = await Db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == customer.CustomerId.ToString());
                if (notification != null) { notification.IsRead = true; await Db.SaveChangesAsync(); }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var customer = await CurrentCustomerAsync();
            if (customer != null)
            {
                var notifications = await Db.Notifications.Where(n => n.UserId == customer.CustomerId.ToString() && !n.IsRead).ToListAsync();
                foreach (var n in notifications) n.IsRead = true;
                await Db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var customer = await CurrentCustomerAsync();
            if (customer == null) return Json(new { count = 0 });
            var count = await Db.Notifications.CountAsync(n => n.UserId == customer.CustomerId.ToString() && !n.IsRead);
            return Json(new { count });
        }
    }
}
