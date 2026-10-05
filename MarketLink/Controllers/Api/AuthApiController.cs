using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using MarketLink.Models;
using MarketLink.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MarketLink.Controllers.Api;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthApiController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AccountService _accountService;

    public AuthApiController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, AccountService accountService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _accountService = accountService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] ApiLoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid credentials format.", errors = ModelState });

        var outcome = await _accountService.SignInAsync(request.Email, request.Password, rememberMe: false);
        if (!outcome.Succeeded)
            return Unauthorized(new { success = false, message = outcome.Message ?? "Invalid login attempt." });

        var roles = await _userManager.GetRolesAsync(outcome.User!);

        return Ok(new
        {
            success = true,
            message = "Authentication successful.",
            redirectUrl = await _accountService.ResolveRedirectAsync(outcome.User!),
            user = new
            {
                outcome.User!.Id,
                outcome.User.Email,
                outcome.User.FirstName,
                outcome.User.LastName,
                FullName = $"{outcome.User.FirstName} {outcome.User.LastName}".Trim(),
                Roles = roles
            }
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] ApiRegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Validation failed.", errors = ModelState });

        var role = AccountService.NormalizeRole(request.Role);
        var result = await _accountService.RegisterAsync(new RegisterRequest(
            request.Email, request.Password, request.FirstName, request.LastName,
            request.PhoneNumber, request.City, request.AddressLine1, request.State, request.PostalCode, role, request.FarmName));

        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors.Select(e => e.Description) });

        var user = await _userManager.FindByEmailAsync(request.Email);
        return StatusCode(201, new
        {
            success = true,
            message = role == "Farmer"
                ? "Seller account created. Your farm is pending admin approval before orders open."
                : "Account registered successfully.",
            userId = user?.Id,
            role
        });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { success = false, message = "Not authenticated." });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { success = false, message = "User not found." });

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            success = true,
            user = new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.CreatedAt,
                Roles = roles
            }
        });
    }
}

public class ApiLoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class ApiRegisterRequest
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }
    [Required]
    public string AddressLine1 { get; set; } = string.Empty;
    [Required]
    public string City { get; set; } = string.Empty;
    [Required]
    public string State { get; set; } = string.Empty;
    [Required]
    public string PostalCode { get; set; } = string.Empty;
    public string? FarmName { get; set; }
    public string? Role { get; set; } = "Customer";
}
