using MarketLink.Models;
using MarketLink.Models.Enums;
using MarketLink.Repositories;
using MarketLink.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Services;

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Phone,
    string? City,
    string? AddressLine1,
    string? State,
    string? PostalCode,
    string Role,
    string? FarmName);

public record SignInOutcome(bool Succeeded, ApplicationUser? User, string? Message);





public class AccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IUnitOfWork _unitOfWork;

    public AccountService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _unitOfWork = unitOfWork;
    }

    public static string NormalizeRole(string? role) =>
        string.Equals(role, "Farmer", StringComparison.OrdinalIgnoreCase) ? "Farmer" : "Customer";

    public async Task<IdentityResult> RegisterAsync(RegisterRequest request)
    {
        var role = NormalizeRole(request.Role);
        var email = (request.Email ?? string.Empty).Trim();

        if (await _userManager.FindByEmailAsync(email) is not null)
            return IdentityResult.Failed(new IdentityError { Description = "An account with this email already exists." });

        if (role == "Farmer" && string.IsNullOrWhiteSpace(request.FarmName))
            return IdentityResult.Failed(new IdentityError { Description = "Farm name is required for seller accounts." });

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = (request.FirstName ?? string.Empty).Trim(),
            LastName = (request.LastName ?? string.Empty).Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded) return created;

        try
        {
            var added = await _userManager.AddToRoleAsync(user, role);
            if (!added.Succeeded) throw new InvalidOperationException(string.Join(" ", added.Errors.Select(e => e.Description)));

            await CreateProfileAsync(user, role, request.FarmName, request.City, request.AddressLine1, request.State, request.PostalCode);
            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            await _userManager.DeleteAsync(user);
            return IdentityResult.Failed(new IdentityError { Description = ex.Message });
        }
    }

    public async Task<SignInOutcome> SignInAsync(string email, string password, bool rememberMe)
    {
        var user = await _userManager.FindByEmailAsync((email ?? string.Empty).Trim());
        if (user == null) return new SignInOutcome(false, null, "Invalid login attempt.");
        if (!user.IsActive) return new SignInOutcome(false, null, "This account has been deactivated. Please contact support.");

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("Farmer", StringComparer.OrdinalIgnoreCase))
        {
            var farmer = await _unitOfWork.Repository<Farmer>().Query()
                .FirstOrDefaultAsync(f => f.UserId == user.Id);

            if (farmer?.Status == FarmerStatus.Suspended)
                return new SignInOutcome(false, null, "Your farmer account is suspended. Please contact an administrator.");
        }

        await EnsureProfileAsync(user);

        var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            var message = result.IsLockedOut ? "Account temporarily locked. Try again later." : "Invalid login attempt.";
            return new SignInOutcome(false, null, message);
        }

        return new SignInOutcome(true, user, null);
    }

    public async Task<string> ResolveRedirectAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("Admin")) return UrlArea("Admin", "Dashboard");
        if (roles.Contains("Farmer")) return UrlArea("Farmer", "Dashboard");
        return UrlArea("Customer", "Dashboard");
    }

    private static string UrlArea(string area, string controller) => $"/{area}/{controller}";

    private async Task CreateProfileAsync(ApplicationUser user, string role, string? farmName, string? city, string? addressLine1, string? state, string? postalCode)
    {
        if (role == "Farmer")
        {
            await _unitOfWork.Repository<Farmer>().AddAsync(new Farmer
            {
                UserId = user.Id,
                FarmName = (farmName ?? string.Empty).Trim().Length > 0
                    ? farmName!.Trim()
                    : $"{user.FullName} Farm",
                Phone = user.PhoneNumber,
                Address = addressLine1?.Trim(),
                City = city?.Trim(),
                State = state?.Trim(),
                Status = FarmerStatus.Pending,
                RegisteredAt = DateTime.UtcNow
            });
        }
        else
        {
            var customer = new Customer
            {
                UserId = user.Id,
                DefaultCity = string.IsNullOrWhiteSpace(city) ? null : city.Trim()
            };
            await _unitOfWork.Repository<Customer>().AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.Repository<CustomerAddress>().AddAsync(new CustomerAddress
            {
                CustomerId = customer.Id,
                Label = "Home",
                AddressLine1 = (addressLine1 ?? string.Empty).Trim(),
                City = (city ?? string.Empty).Trim(),
                State = (state ?? string.Empty).Trim(),
                PostalCode = (postalCode ?? string.Empty).Trim(),
                IsDefault = true
            });
        }

        await _unitOfWork.SaveChangesAsync();
    }



    private async Task EnsureProfileAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains("Farmer") && !await _unitOfWork.Repository<Farmer>().AnyAsync(f => f.UserId == user.Id))
        {
            await _unitOfWork.Repository<Farmer>().AddAsync(new Farmer
            {
                UserId = user.Id,
                FarmName = $"{(string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName)} Farm",
                Phone = user.PhoneNumber,
                Status = FarmerStatus.Pending,
                RegisteredAt = DateTime.UtcNow
            });
        }

        if (roles.Contains("Customer") && !await _unitOfWork.Repository<Customer>().AnyAsync(c => c.UserId == user.Id))
        {
            await _unitOfWork.Repository<Customer>().AddAsync(new Customer { UserId = user.Id });
        }

        await _unitOfWork.SaveChangesAsync();
    }
}
