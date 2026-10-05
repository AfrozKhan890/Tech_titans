using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MarketLink.Repositories;
using MarketLink.Services;
using MarketLink.Models;
using FarmerModel = global::MarketLink.Models.Farmer;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
public class FavoritesController : MarketLink.Services.CustomerAreaController
{
    private readonly IUnitOfWork _unitOfWork;

    public FavoritesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "My Favorites";
        int customerId = CustomerId;

        var favorites = await _unitOfWork.Repository<Favorite>().Query()
            .Include(f => f.Product).ThenInclude(p => p!.Category)
            .Include(f => f.Product).ThenInclude(p => p!.Farmer)
            .Include(f => f.Product).ThenInclude(p => p!.Images)
            .Include(f => f.Farmer).ThenInclude(farmer => farmer!.User)
            .Include(f => f.Market)
            .Where(f => f.CustomerId == customerId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        return View(favorites);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(string type, int id)
    {
        if (string.IsNullOrEmpty(type) || id <= 0)
        {
            return Json(new { success = false, message = "Invalid parameters." });
        }

        try
        {
            int customerId = CustomerId;
            var favRepo = _unitOfWork.Repository<Favorite>();

            Favorite? existing = null;
            if (type.ToLower() == "product")
            {
                var favs = await favRepo.FindAsync(f => f.CustomerId == customerId && f.ProductId == id);
                existing = favs.FirstOrDefault();
            }
            else if (type.ToLower() == "farmer")
            {
                var farmer = await _unitOfWork.Repository<FarmerModel>().FirstOrDefaultAsync(f =>
                    f.Id == id && f.Status == MarketLink.Models.Enums.FarmerStatus.Approved);
                if (farmer == null) return Json(new { success = false, message = "Farmer not found." });
                var favs = await favRepo.FindAsync(f => f.CustomerId == customerId && f.FarmerId == id);
                existing = favs.FirstOrDefault();
            }
            else if (type.ToLower() == "market")
            {
                var market = await _unitOfWork.Repository<Market>().FirstOrDefaultAsync(m => m.Id == id && m.IsActive);
                if (market == null) return Json(new { success = false, message = "Market not found." });
                var favs = await favRepo.FindAsync(f => f.CustomerId == customerId && f.MarketId == id);
                existing = favs.FirstOrDefault();
            }
            else
            {
                return Json(new { success = false, message = "Invalid type." });
            }

            bool isFavorite;
            if (existing != null)
            {
                favRepo.Remove(existing);
                isFavorite = false;
            }
            else
            {
                var newFav = new Favorite
                {
                    CustomerId = customerId,
                    CreatedAt = DateTime.UtcNow
                };

                if (type.ToLower() == "product") newFav.ProductId = id;
                if (type.ToLower() == "farmer") newFav.FarmerId = id;
                if (type.ToLower() == "market") newFav.MarketId = id;

                await favRepo.AddAsync(newFav);
                isFavorite = true;
            }

            await _unitOfWork.SaveChangesAsync();

            return Json(new { success = true, isFavorite });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id)
    {
        int customerId = CustomerId;
        var favRepo = _unitOfWork.Repository<Favorite>();
        var fav = await favRepo.GetByIdAsync(id);

        if (fav != null && fav.CustomerId == customerId)
        {
            favRepo.Remove(fav);
            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = "Item removed from favorites.";
        }

        return RedirectToAction(nameof(Index));
    }
}
