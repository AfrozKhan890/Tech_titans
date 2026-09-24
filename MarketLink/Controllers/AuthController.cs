using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using MarketLink.Areas.Customer;

namespace MarketLink.Controllers
{
    // Compatibility facade for public links that still use /Auth/*.
    // Customer authentication itself lives in Areas/Customer and uses CustomerAuth.
    public class AuthController : Controller
    {
        [HttpGet]
        public IActionResult Login(string? returnUrl = null) =>
            RedirectToAction("Login", "Account", new { area = "Customer", returnUrl });

        [HttpGet]
        public IActionResult Register() =>
            RedirectToAction("Register", "Account", new { area = "Customer" });

        [HttpGet]
        public IActionResult AccessDenied() =>
            RedirectToAction("AccessDenied", "Account", new { area = "Customer" });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CustomerAuthDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}
