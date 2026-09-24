using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MarketLink.Services;
using MarketLink.Areas.Admin.Services;
using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;
using MarketLink.Areas.Admin;

namespace MarketLink.Areas.Admin.Controllers
{
    /// <summary>
    /// Admin-facing, read-only view over orders. Per the SRS, accepting,
    /// declining, and marking orders ready for pickup are Farmer actions —
    /// the Admin panel's role here is oversight (view, search, filter),
    /// not order-status management.
    /// </summary>
    [Authorize(AuthenticationSchemes = AdminAuthDefaults.AuthenticationScheme, Policy = AdminAuthDefaults.AdminPolicy)]
    [Area("Admin")]
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        // GET: /Orders
        public async Task<IActionResult> Index(OrderFilterViewModel filter)
        {
            var vm = await _orderService.GetOrdersAsync(filter);
            return View(vm);
        }

        // GET: /Orders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var vm = await _orderService.GetOrderDetailsAsync(id);
            if (vm == null)
            {
                return NotFound();
            }
            return View(vm);
        }
    }
}
