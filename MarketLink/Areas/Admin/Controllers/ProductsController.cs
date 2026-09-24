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
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        // GET: /Products
        public async Task<IActionResult> Index(ProductFilterViewModel filter)
        {
            var vm = await _productService.GetProductsAsync(filter);
            return View(vm);
        }

        // GET: /Products/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var vm = await _productService.GetProductDetailsAsync(id);
            if (vm == null)
            {
                return NotFound();
            }
            return View(vm);
        }

        // POST: /Products/Remove/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id, string? returnUrl = null)
        {
            var success = await _productService.ChangeStatusAsync(id, ProductStatus.Removed);
            if (!success)
            {
                return NotFound();
            }
            TempData["SuccessMessage"] = "Listing removed for violating platform guidelines.";
            return RedirectBack(returnUrl, id);
        }

        // POST: /Products/Activate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id, string? returnUrl = null)
        {
            var success = await _productService.ChangeStatusAsync(id, ProductStatus.Active);
            if (!success)
            {
                return NotFound();
            }
            TempData["SuccessMessage"] = "Listing has been activated.";
            return RedirectBack(returnUrl, id);
        }

        // POST: /Products/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id, string? returnUrl = null)
        {
            var success = await _productService.ChangeStatusAsync(id, ProductStatus.Inactive);
            if (!success)
            {
                return NotFound();
            }
            TempData["SuccessMessage"] = "Listing has been deactivated.";
            return RedirectBack(returnUrl, id);
        }

        private IActionResult RedirectBack(string? returnUrl, int productId)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Details), new { id = productId });
        }
    }
}
