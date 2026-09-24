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
    public class FarmersController : Controller
    {
        private readonly IFarmerService _farmerService;

        public FarmersController(IFarmerService farmerService)
        {
            _farmerService = farmerService;
        }

        // GET: /Farmers
        public async Task<IActionResult> Index(FarmerFilterViewModel filter)
        {
            var vm = await _farmerService.GetFarmersAsync(filter);
            return View(vm);
        }

        // GET: /Farmers/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var vm = await _farmerService.GetFarmerDetailsAsync(id);
            if (vm == null)
            {
                return NotFound();
            }
            return View(vm);
        }

        // GET: /Farmers/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var vm = await _farmerService.GetFarmerForEditAsync(id);
            if (vm == null)
            {
                return NotFound();
            }
            return View(vm);
        }

        // POST: /Farmers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FarmerEditViewModel model)
        {
            if (id != model.FarmerId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                model.Markets = await _farmerService.GetMarketsAsync();
                return View(model);
            }

            var success = await _farmerService.UpdateFarmerAsync(model);
            if (!success)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = $"Farmer \"{model.StallName}\" was updated successfully.";
            return RedirectToAction(nameof(Details), new { id = model.FarmerId });
        }

        // POST: /Farmers/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? returnUrl = null)
        {
            await _farmerService.ChangeStatusAsync(id, FarmerStatus.Approved);
            TempData["SuccessMessage"] = "Farmer registration approved.";
            return RedirectBack(returnUrl, id);
        }

        // POST: /Farmers/Suspend/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspend(int id, string? returnUrl = null)
        {
            await _farmerService.ChangeStatusAsync(id, FarmerStatus.Suspended);
            TempData["SuccessMessage"] = "Farmer has been suspended.";
            return RedirectBack(returnUrl, id);
        }

        // POST: /Farmers/Activate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id, string? returnUrl = null)
        {
            await _farmerService.ChangeStatusAsync(id, FarmerStatus.Approved);
            TempData["SuccessMessage"] = "Farmer has been activated.";
            return RedirectBack(returnUrl, id);
        }

        // POST: /Farmers/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id, string? returnUrl = null)
        {
            await _farmerService.ChangeStatusAsync(id, FarmerStatus.Inactive);
            TempData["SuccessMessage"] = "Farmer has been deactivated.";
            return RedirectBack(returnUrl, id);
        }

        private IActionResult RedirectBack(string? returnUrl, int farmerId)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Details), new { id = farmerId });
        }
    }
}
