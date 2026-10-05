using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MarketLink.Repositories;
using MarketLink.Services;
using MarketLink.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MarketLink.Controllers.Api;

[ApiController]
[Authorize(Roles = "Customer")]
[Route("api/favorites")]
[Produces("application/json")]
public class FavoritesApiController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public FavoritesApiController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyFavorites()
    {
        var customerId = await ActorResolver.ResolveCustomerIdAsync(User, _unitOfWork, HttpContext);
        if (customerId == null) return NotFound(new { success = false, message = "Customer profile not found for this account." });

        var favs = await _unitOfWork.Repository<Favorite>().Query()
            .Include(f => f.Product).ThenInclude(p => p!.Category)
            .Include(f => f.Farmer)
            .Include(f => f.Market)
            .Where(f => f.CustomerId == customerId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new
            {
                f.Id,
                f.ProductId,
                f.FarmerId,
                f.MarketId,
                f.CreatedAt,
                Product = f.Product != null ? new
                {
                    f.Product.Id,
                    f.Product.Name,
                    f.Product.PricePerKg,
                    f.Product.ImageUrl,
                    CategoryName = f.Product.Category.Name
                } : null,
                Farmer = f.Farmer != null ? new
                {
                    f.Farmer.Id,
                    f.Farmer.FarmName,
                    f.Farmer.Bio
                } : null,
                Market = f.Market != null ? new
                {
                    f.Market.Id,
                    f.Market.Name,
                    f.Market.Address,
                    f.Market.City,
                    f.Market.State
                } : null
            })
            .ToListAsync();

        return Ok(new { success = true, count = favs.Count, favorites = favs });
    }

    [HttpPost("toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFavorite([FromBody] ApiToggleFavoriteRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, errors = ModelState });

        var customerId = await ActorResolver.ResolveCustomerIdAsync(User, _unitOfWork, HttpContext);
        if (customerId == null) return NotFound(new { success = false, message = "Customer profile not found for this account." });

        if (request.Id <= 0)
            return BadRequest(new { success = false, message = "An id is required." });


        var type = request.Type.ToLower();
        if (type == "product" && !await _unitOfWork.Repository<Product>().AnyAsync(p => p.Id == request.Id))
            return NotFound(new { success = false, message = "That produce is no longer listed." });
        if (type == "farmer" && !await _unitOfWork.Repository<Farmer>().AnyAsync(f => f.Id == request.Id && f.Status == MarketLink.Models.Enums.FarmerStatus.Approved))
            return NotFound(new { success = false, message = "That farm does not exist." });
        if (type == "market" && !await _unitOfWork.Repository<Market>().AnyAsync(m => m.Id == request.Id && m.IsActive))
            return NotFound(new { success = false, message = "That market does not exist." });

        var repo = _unitOfWork.Repository<Favorite>();
        Favorite? existing = null;

        if (type == "product")
        {
            var results = await repo.FindAsync(f => f.CustomerId == customerId && f.ProductId == request.Id);
            existing = results.FirstOrDefault();
        }
        else if (type == "farmer")
        {
            var results = await repo.FindAsync(f => f.CustomerId == customerId && f.FarmerId == request.Id);
            existing = results.FirstOrDefault();
        }
        else if (type == "market")
        {
            var results = await repo.FindAsync(f => f.CustomerId == customerId && f.MarketId == request.Id);
            existing = results.FirstOrDefault();
        }
        else
        {
            return BadRequest(new { success = false, message = "Type must be 'product', 'farmer', or 'market'." });
        }

        bool isFavorite;
        if (existing != null)
        {
            repo.Remove(existing);
            isFavorite = false;
        }
        else
        {
            var newFav = new Favorite
            {
                CustomerId = customerId.Value,
                CreatedAt = DateTime.UtcNow
            };
            if (type == "product") newFav.ProductId = request.Id;
            if (type == "farmer") newFav.FarmerId = request.Id;
            if (type == "market") newFav.MarketId = request.Id;

            await repo.AddAsync(newFav);
            isFavorite = true;
        }

        await _unitOfWork.SaveChangesAsync();
        return Ok(new { success = true, isFavorite, message = isFavorite ? "Saved to favorites." : "Removed from favorites." });
    }
}

public class ApiToggleFavoriteRequest
{
    [Required]
    public string Type { get; set; } = "product";
    [Required]
    public int Id { get; set; }
}
