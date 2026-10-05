using Microsoft.AspNetCore.Mvc;
using MarketLink.Models;
using MarketLink.Models.Enums;
using MarketLink.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarketLink.Controllers;

public class FarmersController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public FarmersController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<IActionResult> Index()
    {
        var farmers = await _unitOfWork.Repository<Farmer>().Query()
            .Where(f => f.Status == FarmerStatus.Approved)
            .OrderBy(f => f.FarmName)
            .ToListAsync();
        return View(farmers);
    }

    public async Task<IActionResult> Details(int id)
    {
        var farmer = await _unitOfWork.Repository<Farmer>().Query()
            .Include(f => f.FarmerMarkets).ThenInclude(fm => fm.Market)
            .Include(f => f.Products).ThenInclude(p => p.Category)
            .Include(f => f.Products).ThenInclude(p => p.Images)
            .Include(f => f.PickupSlots).ThenInclude(s => s.Market)
            .Include(f => f.Reviews).ThenInclude(r => r.Customer).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(f => f.Id == id && f.Status == FarmerStatus.Approved);
        if (farmer == null) return NotFound();

        var products = farmer.Products
            .Where(p => p.IsAvailable && p.StockQuantityKg > 0)
            .OrderBy(p => p.Name)
            .ToList();
        var markets = farmer.FarmerMarkets
            .Where(fm => fm.IsActive && fm.Market.IsActive)
            .OrderBy(fm => fm.Market.Name)
            .Select(fm => fm.Market)
            .ToList();
        var slots = farmer.PickupSlots
            .Where(s => s.IsActive && (s.Market == null || s.Market.IsActive))
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
            .ToList();

        ViewBag.Products = products;
        var canReviewFarmer = false;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var customer = string.IsNullOrEmpty(userId) ? null : await _unitOfWork.Repository<Customer>().FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer != null)
                canReviewFarmer = await _unitOfWork.Repository<Order>().AnyAsync(o => o.CustomerId == customer.Id && o.FarmerId == id && o.Status == OrderStatus.Completed);
        }
        ViewBag.CanReviewFarmer = canReviewFarmer;
        ViewBag.Markets = markets;
        ViewBag.PickupSlots = slots;
        ViewBag.Reviews = farmer.Reviews.Where(r => r.IsApproved).OrderByDescending(r => r.CreatedAt).ToList();
        return View(farmer);
    }
}
