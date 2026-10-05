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
public class ReviewsController : MarketLink.Services.FarmerAreaController
{
    private readonly IUnitOfWork _unitOfWork;

    public ReviewsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Customer Reviews";
        int farmerId = FarmerId;

        var reviews = await _unitOfWork.Repository<Review>().Query()
            .Include(r => r.Customer)
                .ThenInclude(c => c.User)
            .Include(r => r.Product)
            .Where(r => r.FarmerId == farmerId || (r.Product != null && r.Product.FarmerId == farmerId))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        ViewBag.AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 5.0;
        ViewBag.ReviewCount = reviews.Count;

        return View(reviews);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, string farmerResponse)
    {
        int farmerId = FarmerId;

        var review = await _unitOfWork.Repository<Review>().Query()
            .Include(r => r.Product)
            .FirstOrDefaultAsync(r => r.Id == id && (r.FarmerId == farmerId || (r.Product != null && r.Product.FarmerId == farmerId)));

        if (review != null)
        {
            review.FarmerResponse = farmerResponse?.Trim();
            review.FarmerRespondedAt = DateTime.UtcNow;

            await _unitOfWork.Repository<Review>().UpdateAsync(review);
            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = "Response saved and published!";
        }
        else
        {
            TempData["Error"] = "Review not found.";
        }

        return RedirectToAction(nameof(Index));
    }
}
