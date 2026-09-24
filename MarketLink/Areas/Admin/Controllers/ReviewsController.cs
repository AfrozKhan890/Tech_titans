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
    /// Admin review moderation: view, search/filter, and remove
    /// reviews that violate platform guidelines. Reviews are soft
    /// removed (IsRemoved flag) rather than deleted, so moderation
    /// history is preserved and a removal can be reversed.
    /// </summary>
    [Authorize(AuthenticationSchemes = AdminAuthDefaults.AuthenticationScheme, Policy = AdminAuthDefaults.AdminPolicy)]
    [Area("Admin")]
    public class ReviewsController : Controller
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        // GET: /Reviews
        public async Task<IActionResult> Index(ReviewFilterViewModel filter)
        {
            var vm = await _reviewService.GetReviewsAsync(filter);
            return View(vm);
        }

        // GET: /Reviews/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var vm = await _reviewService.GetReviewDetailsAsync(id);
            if (vm == null)
            {
                return NotFound();
            }
            return View(vm);
        }

        // POST: /Reviews/Remove/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id, string? returnUrl = null)
        {
            var success = await _reviewService.SetRemovedAsync(id, true);
            if (!success)
            {
                return NotFound();
            }
            TempData["SuccessMessage"] = "Review removed for violating platform guidelines.";
            return RedirectBack(returnUrl, id);
        }

        // POST: /Reviews/Restore/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id, string? returnUrl = null)
        {
            var success = await _reviewService.SetRemovedAsync(id, false);
            if (!success)
            {
                return NotFound();
            }
            TempData["SuccessMessage"] = "Review restored and is visible again.";
            return RedirectBack(returnUrl, id);
        }

        private IActionResult RedirectBack(string? returnUrl, int reviewId)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Details), new { id = reviewId });
        }
    }
}
