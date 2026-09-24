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
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly IAdminAuthService _authService;

        public AccountController(IAdminAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            var adminAuth = await HttpContext.AuthenticateAsync(AdminAuthDefaults.AuthenticationScheme);
            if (adminAuth.Succeeded && adminAuth.Principal?.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var model = new LoginViewModel { ReturnUrl = returnUrl };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var admin = await _authService.ValidateCredentialsAsync(model.Email, model.Password);

            if (admin == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password. Please try again.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, admin.AdminId.ToString()),
                new Claim(ClaimTypes.Name, admin.FullName),
                new Claim(ClaimTypes.Email, admin.Email),
                new Claim(ClaimTypes.Role, admin.Role)
            };

            var identity = new ClaimsIdentity(claims, AdminAuthDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                AdminAuthDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            await _authService.UpdateLastLoginAsync(admin.AdminId);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        [Authorize(AuthenticationSchemes = AdminAuthDefaults.AuthenticationScheme, Policy = AdminAuthDefaults.AdminPolicy)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(AdminAuthDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
