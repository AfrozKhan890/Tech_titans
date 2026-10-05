using MarketLink.Models;
using MarketLink.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Areas.Farmer.Controllers;

[Area("Farmer")]
[Authorize(Roles = "Farmer")]
public class PickupSlotsController : MarketLink.Services.FarmerAreaController
{
    private readonly IUnitOfWork _unitOfWork;

    public PickupSlotsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Pickup Slots";
        int farmerId = FarmerId;

        var slots = await _unitOfWork.Repository<PickupSlot>().Query()
            .Include(ps => ps.Market)
            .Where(ps => ps.FarmerId == farmerId)
            .OrderBy(ps => ps.DayOfWeek)
            .ThenBy(ps => ps.StartTime)
            .ToListAsync();

        var markets = await _unitOfWork.Repository<Market>().Query()
            .Where(m => m.FarmerMarkets.Any(fm => fm.FarmerId == farmerId && fm.IsActive))
            .OrderBy(m => m.Name)
            .ToListAsync();
        ViewBag.Markets = markets;

        return View(slots);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int marketId, DayOfWeek dayOfWeek, string startTime, string endTime, string? cutoffTime, int maxOrders = 20, string? notes = null)
    {
        int farmerId = FarmerId;

        if (!TimeOnly.TryParse(startTime, out var start) || !TimeOnly.TryParse(endTime, out var end))
        {
            TempData["Error"] = "Invalid pickup time format.";
            return RedirectToAction(nameof(Index));
        }

        if (end <= start)
        {
            TempData["Error"] = "Pickup end time must be later than the start time.";
            return RedirectToAction(nameof(Index));
        }

        TimeOnly? cutoff = null;
        if (!string.IsNullOrWhiteSpace(cutoffTime))
        {
            if (!TimeOnly.TryParse(cutoffTime, out var parsedCutoff) || parsedCutoff > start)
            {
                TempData["Error"] = "Cutoff time must be on or before the pickup start time.";
                return RedirectToAction(nameof(Index));
            }
            cutoff = parsedCutoff;
        }
        else
        {
            cutoff = start;
        }

        var marketLinked = await _unitOfWork.Repository<FarmerMarket>().AnyAsync(fm =>
            fm.FarmerId == farmerId && fm.MarketId == marketId && fm.IsActive);
        if (!marketLinked)
        {
            TempData["Error"] = "You can only create pickup slots for markets linked to your farm.";
            return RedirectToAction(nameof(Index));
        }

        if (maxOrders <= 0 || maxOrders > 1000)
        {
            TempData["Error"] = "Pickup capacity must be between 1 and 1000 orders.";
            return RedirectToAction(nameof(Index));
        }

        var slot = new PickupSlot
        {
            FarmerId = farmerId,
            MarketId = marketId,
            DayOfWeek = dayOfWeek,
            StartTime = start,
            EndTime = end,
            CutoffTime = cutoff,
            MaxOrders = maxOrders,
            IsActive = true,
            Notes = notes?.Trim()
        };

        await _unitOfWork.Repository<PickupSlot>().AddAsync(slot);
        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = "New pickup window added successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var slot = await _unitOfWork.Repository<PickupSlot>().Query()
            .FirstOrDefaultAsync(s => s.Id == id && s.FarmerId == FarmerId);

        if (slot != null)
        {
            slot.IsActive = !slot.IsActive;
            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = slot.IsActive ? "Slot enabled." : "Slot disabled.";
        }
        else
        {
            TempData["Error"] = "Pickup slot not found.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var slot = await _unitOfWork.Repository<PickupSlot>().Query()
            .FirstOrDefaultAsync(s => s.Id == id && s.FarmerId == FarmerId);

        if (slot == null)
        {
            TempData["Error"] = "Pickup slot not found.";
            return RedirectToAction(nameof(Index));
        }

        var hasActiveOrders = await _unitOfWork.Repository<Order>().AnyAsync(o =>
            o.PickupSlotId == id && o.Status != MarketLink.Models.Enums.OrderStatus.Cancelled && o.Status != MarketLink.Models.Enums.OrderStatus.Completed);
        if (hasActiveOrders)
        {
            TempData["Error"] = "This pickup slot has active orders and cannot be deleted. Disable it instead.";
            return RedirectToAction(nameof(Index));
        }

        _unitOfWork.Repository<PickupSlot>().Remove(slot);
        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = "Pickup slot removed.";
        return RedirectToAction(nameof(Index));
    }
}
