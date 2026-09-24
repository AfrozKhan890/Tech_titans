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
    public class MarketsController : Controller
    {
        private readonly IMarketService _marketService;

        public MarketsController(IMarketService marketService)
        {
            _marketService = marketService;
        }

        // GET: /Markets
        public async Task<IActionResult> Index(MarketFilterViewModel filter)
        {
            var vm = await _marketService.GetMarketsAsync(filter);
            return View(vm);
        }

        // GET: /Markets/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var vm = await _marketService.GetMarketDetailsAsync(id);
            if (vm == null)
            {
                return NotFound();
            }
            return View(vm);
        }

        // GET: /Markets/Create
        public IActionResult Create()
        {
            return View(new MarketCreateViewModel());
        }

        // POST: /Markets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MarketCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var newId = await _marketService.CreateMarketAsync(model);
            TempData["SuccessMessage"] = $"Market \"{model.MarketName}\" was created successfully.";
            return RedirectToAction(nameof(Details), new { id = newId });
        }

        // GET: /Markets/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var vm = await _marketService.GetMarketForEditAsync(id);
            if (vm == null)
            {
                return NotFound();
            }
            return View(vm);
        }

        // POST: /Markets/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MarketEditViewModel model)
        {
            if (id != model.MarketId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var success = await _marketService.UpdateMarketAsync(model);
            if (!success)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = $"Market \"{model.MarketName}\" was updated successfully.";
            return RedirectToAction(nameof(Details), new { id = model.MarketId });
        }

        // POST: /Markets/Activate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id, string? returnUrl = null)
        {
            var success = await _marketService.ChangeStatusAsync(id, MarketStatus.Active);
            if (!success)
            {
                return NotFound();
            }
            TempData["SuccessMessage"] = "Market has been activated.";
            return RedirectBack(returnUrl, id);
        }

        // POST: /Markets/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id, string? returnUrl = null)
        {
            var success = await _marketService.ChangeStatusAsync(id, MarketStatus.Inactive);
            if (!success)
            {
                return NotFound();
            }
            TempData["SuccessMessage"] = "Market has been deactivated.";
            return RedirectBack(returnUrl, id);
        }

        // POST: /Markets/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, farmerCount) = await _marketService.DeleteMarketAsync(id);
            if (!success)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = farmerCount > 0
                ? $"Market deleted. {farmerCount} farmer(s) previously assigned to it are now unassigned."
                : "Market deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private IActionResult RedirectBack(string? returnUrl, int marketId)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Details), new { id = marketId });
        }
    }
}
