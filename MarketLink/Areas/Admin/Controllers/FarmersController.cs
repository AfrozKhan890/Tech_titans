using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MarketLink.Repositories;
using MarketLink.Services;
using MarketLink.Models.Enums;
using Microsoft.EntityFrameworkCore;
using FarmerEntity = MarketLink.Models.Farmer;

namespace MarketLink.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class FarmersController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFarmerService _farmerService;

    public FarmersController(IUnitOfWork unitOfWork, IFarmerService farmerService)
    {
        _unitOfWork = unitOfWork;
        _farmerService = farmerService;
    }

    public async Task<IActionResult> Index(string? status)
    {
        ViewData["Title"] = "Farmer Management";

        FarmerStatus? filterStatus = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<FarmerStatus>(status, true, out var parsed))
            filterStatus = parsed;

        var farmers = await _unitOfWork.Repository<FarmerEntity>().Query()
            .Include(f => f.User)
            .Include(f => f.Products)
            .Include(f => f.FarmerMarkets)
                .ThenInclude(fm => fm.Market)
            .Where(f => !filterStatus.HasValue || f.Status == filterStatus.Value)
            .OrderByDescending(f => f.RegisteredAt)
            .ToListAsync();

        ViewBag.CurrentStatus = status;
        ViewBag.PendingCount = await _unitOfWork.Repository<FarmerEntity>()
            .CountAsync(f => f.Status == FarmerStatus.Pending);

        return View(farmers);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var farmer = await _unitOfWork.Repository<FarmerEntity>().GetByIdAsync(id);
        if (farmer != null)
        {
            farmer.Status = FarmerStatus.Approved;
            await _unitOfWork.Repository<FarmerEntity>().UpdateAsync(farmer);
            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = $"Farmer '{farmer.FarmName}' has been approved!";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(int id, string? reason)
    {
        var farmer = await _unitOfWork.Repository<FarmerEntity>().GetByIdAsync(id);
        if (farmer != null)
        {
            farmer.Status = FarmerStatus.Suspended;
            farmer.AdminNotes = reason ?? "Account suspended by administrator.";
            await _unitOfWork.Repository<FarmerEntity>().UpdateAsync(farmer);
            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = $"Farmer '{farmer.FarmName}' has been suspended.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(int id)
    {
        var farmer = await _unitOfWork.Repository<FarmerEntity>().GetByIdAsync(id);
        if (farmer != null && farmer.Status == FarmerStatus.Suspended)
        {
            farmer.Status = FarmerStatus.Approved;
            farmer.AdminNotes = null;
            await _unitOfWork.Repository<FarmerEntity>().UpdateAsync(farmer);
            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = $"Farmer '{farmer.FarmName}' has been reactivated.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFeatured(int id)
    {
        var farmer = await _unitOfWork.Repository<FarmerEntity>().GetByIdAsync(id);
        if (farmer != null)
        {
            farmer.IsFeatured = !farmer.IsFeatured;
            await _unitOfWork.Repository<FarmerEntity>().UpdateAsync(farmer);
            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = farmer.IsFeatured
                ? $"'{farmer.FarmName}' featured on home page."
                : "Removed from featured.";
        }
        return RedirectToAction(nameof(Index));
    }
}
