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
    /// Admin-only platform configuration: general info, notifications,
    /// SMTP credentials, and SMTP verification.
    /// </summary>
    [Authorize(AuthenticationSchemes = AdminAuthDefaults.AuthenticationScheme, Policy = AdminAuthDefaults.AdminPolicy)]
    [Area("Admin")]
    public class SettingsController : Controller
    {
        private readonly ISettingsService _settingsService;

        public SettingsController(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        // GET: /Settings
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = await _settingsService.GetSettingsAsync();
            return View(model);
        }

        // POST: /Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SettingsViewModel model)
        {
            // SMTP password may legitimately be blank to keep the stored one.
            ModelState.Remove(nameof(SettingsViewModel.SmtpPassword));

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _settingsService.UpdateSettingsAsync(model);
            TempData["SuccessMessage"] = "Settings saved successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Settings/SendTestEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTestEmail(string testRecipient)
        {
            if (string.IsNullOrWhiteSpace(testRecipient))
            {
                TempData["ErrorMessage"] = "Please enter an email address to send the test to.";
                return RedirectToAction(nameof(Index));
            }

            var ok = await _settingsService.SendTestEmailAsync(testRecipient);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
                ? $"Test email sent to {testRecipient}."
                : "Failed to send test email. Please verify the SMTP credentials and try again.";

            return RedirectToAction(nameof(Index));
        }
    }
}