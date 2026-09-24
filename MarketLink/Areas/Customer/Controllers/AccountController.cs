using System.Security.Claims;
using MarketLink.Areas.Customer;
using MarketLink.Data;
using MarketLink.Services;
using MarketLink.ViewModels;
using CustomerEntity = MarketLink.Models.Customer;
using CustomerStatus = MarketLink.Models.CustomerStatus;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class AccountController : Controller
    {
        private readonly MarketLinkDbContext _context;
        public AccountController(MarketLinkDbContext context) => _context = context;

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true &&
                User.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "Customer"))
                return RedirectToAction("Index", "Dashboard");

            return View(new CustomerLoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(CustomerLoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var email = model.Email.Trim().ToLowerInvariant();
            var customer = await _context.Customers.FirstOrDefaultAsync(c =>
                c.Email.ToLower() == email && c.Status == CustomerStatus.Active);

            if (customer == null || string.IsNullOrWhiteSpace(customer.PasswordHash) ||
                string.IsNullOrWhiteSpace(customer.PasswordSalt) ||
                !PasswordHasher.VerifyPassword(model.Password, customer.PasswordHash, customer.PasswordSalt))
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            var claims = new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, customer.CustomerId.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, customer.FullName),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, customer.Email),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Customer")
            };

            await HttpContext.SignInAsync(
                CustomerAuthDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CustomerAuthDefaults.AuthenticationScheme)),
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register() => View(new CustomerRegisterViewModel());

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(CustomerRegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var email = model.Email.Trim().ToLowerInvariant();
            if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == email))
            {
                ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
                return View(model);
            }

            var (hash, salt) = PasswordHasher.HashPassword(model.Password);
            var customer = new CustomerEntity
            {
                FullName = model.FullName.Trim(),
                Email = email,
                Phone = model.Phone?.Trim(),
                DefaultCity = model.City?.Trim(),
                PasswordHash = hash,
                PasswordSalt = salt,
                Status = CustomerStatus.Active,
                RegisteredAt = DateTime.UtcNow
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var claims = new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, customer.CustomerId.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, customer.FullName),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, customer.Email),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Customer")
            };

            await HttpContext.SignInAsync(
                CustomerAuthDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CustomerAuthDefaults.AuthenticationScheme)),
                new AuthenticationProperties { IsPersistent = false });

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = CustomerAuthDefaults.AuthenticationScheme, Policy = CustomerAuthDefaults.CustomerPolicy)]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CustomerAuthDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied() => View();
    }
}
