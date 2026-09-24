using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MarketLink.Models;
using MarketLink.Services;
using MarketLink.Areas.Admin.Services;
using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;
using MarketLink.Areas.Admin;

namespace MarketLink.Areas.Admin.Controllers
{
    [Authorize(AuthenticationSchemes = AdminAuthDefaults.AuthenticationScheme, Policy = AdminAuthDefaults.AdminPolicy)]
    [Area("Admin")]
    public class CustomersController : Controller
    {
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        // GET: /Customers
        public async Task<IActionResult> Index(CustomerFilterViewModel filter)
        {
            var vm = await _customerService.GetCustomersAsync(filter);
            return View(vm);
        }

        // GET: /Customers/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var vm = await _customerService.GetCustomerDetailsAsync(id);
            if (vm == null)
            {
                return NotFound();
            }
            return View(vm);
        }

        // POST: /Customers/Activate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id, string? returnUrl = null)
        {
            var success = await _customerService.ChangeStatusAsync(id, CustomerStatus.Active);
            if (!success)
            {
                return NotFound();
            }
            TempData["SuccessMessage"] = "Customer account has been activated.";
            return RedirectBack(returnUrl, id);
        }

        // POST: /Customers/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id, string? returnUrl = null)
        {
            var success = await _customerService.ChangeStatusAsync(id, CustomerStatus.Inactive);
            if (!success)
            {
                return NotFound();
            }
            TempData["SuccessMessage"] = "Customer account has been deactivated.";
            return RedirectBack(returnUrl, id);
        }

        private IActionResult RedirectBack(string? returnUrl, int customerId)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Details), new { id = customerId });
        }
    }
}
