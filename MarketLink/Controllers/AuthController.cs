using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MarketLink.Models;
using MarketLink.Services;

namespace MarketLink.Controllers;

public class AuthController : Controller
{
    private readonly AccountService _account;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthController(AccountService account, SignInManager<ApplicationUser> signInManager)
    {
        _account = account;
        _signInManager = signInManager;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var outcome = await _account.SignInAsync(model.Email, model.Password, model.RememberMe);
        if (!outcome.Succeeded || outcome.User == null)
        {
            ModelState.AddModelError(string.Empty, outcome.Message ?? "Invalid login attempt.");
            return View(model);
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return Redirect(await _account.ResolveRedirectAsync(outcome.User));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string role = "Customer")
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        role = AccountService.NormalizeRole(role);
        return View(new RegisterViewModel { Role = role });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        model.Role = AccountService.NormalizeRole(model.Role);
        if (model.Role == "Farmer" && string.IsNullOrWhiteSpace(model.FarmName))
            ModelState.AddModelError(nameof(model.FarmName), "Farm name is required for seller accounts.");

        if (!ModelState.IsValid) return View(model);

        var result = await _account.RegisterAsync(new RegisterRequest(
            model.Email, model.Password, model.FirstName, model.LastName,
            model.Phone, model.City, model.AddressLine1, model.State, model.PostalCode, model.Role, model.FarmName));

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        var user = await _signInManager.UserManager.FindByEmailAsync(model.Email.Trim());
        if (user != null) await _signInManager.SignInAsync(user, isPersistent: false);

        if (model.Role == "Farmer")
            TempData["Success"] = "Seller account created. Your farm is pending admin approval before its produce appears in the marketplace.";

        return Redirect(user != null ? await _account.ResolveRedirectAsync(user) : Url.Action("Login", "Auth")!);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["Success"] = "You have been signed out.";
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}

public class LoginViewModel
{
    [Required, EmailAddress]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Keep me signed in")]
    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Phone number")]
    public string? Phone { get; set; }

    [Required, StringLength(200)]
    [Display(Name = "Address")]
    public string AddressLine1 { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "City")]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "State / Province")]
    public string State { get; set; } = string.Empty;

    [Required, StringLength(20)]
    [Display(Name = "Postal Code")]
    public string PostalCode { get; set; } = string.Empty;

    [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters and include an uppercase letter and a digit.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password), Compare(nameof(Password))]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Farm / business name")]
    [StringLength(200)]
    public string? FarmName { get; set; }

    public string Role { get; set; } = "Customer";
}
