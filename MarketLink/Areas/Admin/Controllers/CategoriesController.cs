using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MarketLink.Services;
using MarketLink.Areas.Admin.Services;
using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

using MarketLink.Areas.Admin;
namespace MarketLink.Areas.Admin.Controllers
{
    [Authorize(AuthenticationSchemes = AdminAuthDefaults.AuthenticationScheme, Policy = AdminAuthDefaults.AdminPolicy)]
    [Area("Admin")]
    public class CategoriesController : Controller
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // GET: /Categories
        public async Task<IActionResult> Index(CategoryFilterViewModel filter)
        {
            var vm = await _categoryService.GetCategoriesAsync(filter);
            return View(vm);
        }

        // GET: /Categories/Create
        public IActionResult Create()
        {
            return View(new CategoryCreateViewModel());
        }

        // POST: /Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _categoryService.CreateAsync(model);
            TempData["SuccessMessage"] = $"Category \"{model.Name}\" created.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Categories/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var vm = await _categoryService.GetForEditAsync(id);
            if (vm == null)
            {
                return NotFound();
            }
            return View(vm);
        }

        // POST: /Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryEditViewModel model)
        {
            if (id != model.CategoryId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ok = await _categoryService.UpdateAsync(model);
            if (!ok)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = $"Category \"{model.Name}\" updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Categories/Activate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id, string? returnUrl = null)
        {
            await _categoryService.ChangeStatusAsync(id, true);
            TempData["SuccessMessage"] = "Category activated.";
            return RedirectBack(returnUrl);
        }

        // POST: /Categories/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id, string? returnUrl = null)
        {
            await _categoryService.ChangeStatusAsync(id, false);
            TempData["SuccessMessage"] = "Category deactivated.";
            return RedirectBack(returnUrl);
        }

        // POST: /Categories/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, error) = await _categoryService.DeleteAsync(id);
            if (!success)
            {
                TempData["ErrorMessage"] = error ?? "Failed to delete category.";
            }
            else
            {
                TempData["SuccessMessage"] = "Category deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        private IActionResult RedirectBack(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}