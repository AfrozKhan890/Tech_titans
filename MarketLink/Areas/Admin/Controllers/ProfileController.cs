using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
    /// Admin self-service: view/update own profile, change own password.
    /// If the admin changes their email, the auth cookie is refreshed so
    /// the topbar greeting stays in sync.
    /// </summary>
    [Authorize(AuthenticationSchemes = AdminAuthDefaults.AuthenticationScheme, Policy = AdminAuthDefaults.AdminPolicy)]
    [Area("Admin")]
    public class ProfileController : Controller
    {
        private readonly IAdminProfileService _profileService;

        public ProfileController(IAdminProfileService profileService)
        {
            _profileService = profileService;
        }

        private int CurrentAdminId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        // GET: /Profile
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var vm = await _profileService.GetProfileAsync(CurrentAdminId);
            if (vm == null)
            {
                return NotFound();
            }

            // Password change form lives on the same page.
            ViewBag.PasswordModel = new ChangePasswordViewModel();
            return View(vm);
        }

        // POST: /Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AdminProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.PasswordModel = new ChangePasswordViewModel();
                return View(model);
            }

            var (success, error) = await _profileService.UpdateProfileAsync(model);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed to update profile.");
                ViewBag.PasswordModel = new ChangePasswordViewModel();
                return View(model);
            }

            // Refresh auth cookie so the topbar reflects the new name/email.
            await RefreshAuthCookieAsync(model);
            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Profile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var vm = await _profileService.GetProfileAsync(CurrentAdminId);
                if (vm == null)
                {
                    return NotFound();
                }
                ViewBag.PasswordModel = model;
                return View(nameof(Index), vm);
            }

            var (success, error) = await _profileService.ChangePasswordAsync(CurrentAdminId, model);
            if (!success)
            {
                TempData["ErrorMessage"] = error ?? "Failed to change password.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task RefreshAuthCookieAsync(AdminProfileViewModel model)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, model.AdminId.ToString()),
                new Claim(ClaimTypes.Name, model.FullName),
                new Claim(ClaimTypes.Email, model.Email),
                new Claim(ClaimTypes.Role, model.Role)
            };

            var identity = new ClaimsIdentity(claims, AdminAuthDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                AdminAuthDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });
        }
    }
}