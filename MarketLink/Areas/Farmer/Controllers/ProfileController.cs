using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MarketLink.Repositories;
using MarketLink.Services;
using MarketLink.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarketLink.Areas.Farmer.Controllers;

[Area("Farmer")]
[Authorize(Roles = "Farmer")]
public class ProfileController : MarketLink.Services.FarmerAreaController
{
    private readonly IUnitOfWork _unitOfWork;

    public ProfileController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private async Task<MarketLink.Models.Farmer?> GetCurrentFarmerAsync()
    {
        return await _unitOfWork.Repository<MarketLink.Models.Farmer>().Query()
            .Include(f => f.User)
            .Include(f => f.FarmerMarkets)
                .ThenInclude(fm => fm.Market)
            .FirstOrDefaultAsync(f => f.Id == FarmerId);
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Farm Profile";
        var farmer = await GetCurrentFarmerAsync();
        if (farmer == null) return NotFound();
        return View(farmer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(MarketLink.Models.Farmer model)
    {
        var farmer = await GetCurrentFarmerAsync();
        if (farmer == null) return NotFound();

        farmer.FarmName = model.FarmName?.Trim() ?? farmer.FarmName;
        farmer.Description = model.Description?.Trim();
        farmer.Phone = model.Phone?.Trim();
        farmer.Address = model.Address?.Trim();
        farmer.City = model.City?.Trim();
        farmer.State = model.State?.Trim();
        farmer.StallNumber = model.StallNumber?.Trim();
        farmer.Website = model.Website?.Trim();
        farmer.BannerImageUrl = model.BannerImageUrl?.Trim();
        farmer.ProfileImageUrl = model.ProfileImageUrl?.Trim();
        if (model.Latitude.HasValue) farmer.Latitude = model.Latitude;
        if (model.Longitude.HasValue) farmer.Longitude = model.Longitude;

        await _unitOfWork.Repository<MarketLink.Models.Farmer>().UpdateAsync(farmer);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "Farm profile updated successfully!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Markets()
    {
        ViewData["Title"] = "My Markets";
        var farmer = await GetCurrentFarmerAsync();
        if (farmer == null) return NotFound();
        var allMarkets = await _unitOfWork.Repository<Market>().Query().Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync();

        ViewBag.AllMarkets = allMarkets.OrderBy(m => m.Name).ToList();
        return View(farmer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMarket(int marketId, string? stallNumber)
    {
        var farmer = await GetCurrentFarmerAsync();
        if (farmer == null) return NotFound();

        var existing = await _unitOfWork.Repository<FarmerMarket>()
            .FirstOrDefaultAsync(fm => fm.FarmerId == farmer.Id && fm.MarketId == marketId);

        if (existing == null)
        {
            var fm = new FarmerMarket
            {
                FarmerId = farmer.Id,
                MarketId = marketId,
                StallNumber = stallNumber?.Trim(),
                IsActive = true
            };
            await _unitOfWork.Repository<FarmerMarket>().AddAsync(fm);
            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = "Market associated with your farm!";
        }
        else
        {
            TempData["Error"] = "Market is already linked to your farm.";
        }

        return RedirectToAction(nameof(Markets));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMarket(int id)
    {
        var farmer = await GetCurrentFarmerAsync();
        if (farmer == null) return NotFound();
        var fm = await _unitOfWork.Repository<FarmerMarket>().GetByIdAsync(id);

        if (fm != null && fm.FarmerId == farmer.Id)
        {
            _unitOfWork.Repository<FarmerMarket>().Remove(fm);
            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = "Market removed from your profile.";
        }

        return RedirectToAction(nameof(Markets));
    }
}
