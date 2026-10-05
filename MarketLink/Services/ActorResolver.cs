using System.Security.Claims;
using MarketLink.Models;
using MarketLink.Repositories;
using MarketLink.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Services;





public static class ActorResolver
{
    public const string CustomerIdClaim = "ml-customer-id";
    public const string FarmerIdClaim = "ml-farmer-id";

    private const string CustomerCacheKey = "ml.resolved-customer-id";
    private const string FarmerCacheKey = "ml.resolved-farmer-id";

    public static Task<int?> ResolveCustomerIdAsync(ClaimsPrincipal user, IUnitOfWork unitOfWork, HttpContext http)
        => ResolveAsync(user, unitOfWork, http, CustomerIdClaim, CustomerCacheKey,
            (db, userId) => db.Repository<Customer>().Query().Where(c => c.UserId == userId).Select(c => (int?)c.Id).FirstOrDefaultAsync());

    public static Task<int?> ResolveFarmerIdAsync(ClaimsPrincipal user, IUnitOfWork unitOfWork, HttpContext http)
        => ResolveAsync(user, unitOfWork, http, FarmerIdClaim, FarmerCacheKey,
            (db, userId) => db.Repository<Farmer>().Query().Where(f => f.UserId == userId).Select(f => (int?)f.Id).FirstOrDefaultAsync());

    private static async Task<int?> ResolveAsync(
        ClaimsPrincipal user, IUnitOfWork unitOfWork, HttpContext http,
        string claimType, string cacheKey,
        Func<IUnitOfWork, string, Task<int?>> lookup)
    {
        if (http.Items.TryGetValue(cacheKey, out var cached) && cached is int cachedId)
            return cachedId > 0 ? cachedId : null;

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        int? id = null;

        if (!string.IsNullOrEmpty(userId))
        {



            id = await lookup(unitOfWork, userId);
        }

        http.Items[cacheKey] = id ?? -1;
        return id;
    }
}

public abstract class ShopAreaController : Controller
{
    protected IActionResult MissingProfileResult(string returnUrl)
    {
        TempData["Error"] = "Your account profile is missing. Please sign in again to complete it.";
        return RedirectToAction("Login", "Auth", new { area = "", returnUrl });
    }
}

public abstract class CustomerAreaController : ShopAreaController
{
    protected int CustomerId { get; private set; }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var unitOfWork = context.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
        var id = await ActorResolver.ResolveCustomerIdAsync(User, unitOfWork, context.HttpContext);

        if (id == null)
        {
            context.Result = MissingProfileResult(context.HttpContext.Request.Path);
            return;
        }

        CustomerId = id.Value;
        await next();
    }
}

public abstract class FarmerAreaController : ShopAreaController
{
    protected int FarmerId { get; private set; }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var unitOfWork = context.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
        var id = await ActorResolver.ResolveFarmerIdAsync(User, unitOfWork, context.HttpContext);

        if (id == null)
        {
            context.Result = MissingProfileResult(context.HttpContext.Request.Path);
            return;
        }

        FarmerId = id.Value;
        await next();
    }
}
