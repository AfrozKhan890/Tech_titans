using System.Security.Claims;
using MarketLink.Models;
using MarketLink.Repositories;
using MarketLink.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MarketLink.Services;


public class ApplicationUserClaimsFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    private readonly IUnitOfWork _unitOfWork;

    public ApplicationUserClaimsFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options,
        IUnitOfWork unitOfWork)
        : base(userManager, roleManager, options)
    {
        _unitOfWork = unitOfWork;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        var farmer = await _unitOfWork.Repository<Farmer>().Query()
            .Where(f => f.UserId == user.Id)
            .Select(f => new { f.Id, f.FarmName, f.Status })
            .FirstOrDefaultAsync();
        if (farmer != null)
        {
            identity.AddClaim(new Claim(ActorResolver.FarmerIdClaim, farmer.Id.ToString()));
            identity.AddClaim(new Claim("ml-farm-name", farmer.FarmName));
            identity.AddClaim(new Claim("ml-farmer-status", farmer.Status.ToString()));
        }

        var customerId = await _unitOfWork.Repository<Customer>().Query()
            .Where(c => c.UserId == user.Id)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync();
        if (customerId.HasValue)
            identity.AddClaim(new Claim(ActorResolver.CustomerIdClaim, customerId.Value.ToString()));

        return identity;
    }
}
