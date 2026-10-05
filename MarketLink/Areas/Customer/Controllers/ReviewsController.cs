using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MarketLink.Repositories;
using MarketLink.Services;
using MarketLink.Models;
using MarketLink.Models.Enums;
using FarmerModel = global::MarketLink.Models.Farmer;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MarketLink.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
public class ReviewsController : CustomerAreaController
{
    private readonly IUnitOfWork _unitOfWork;

    public ReviewsController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "My Reviews";
        var reviews = await _unitOfWork.Repository<Review>().Query()
            .Include(r => r.Product)
            .Include(r => r.Farmer)
            .Where(r => r.CustomerId == CustomerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        ViewBag.Farmers = await _unitOfWork.Repository<FarmerModel>().Query()
            .Where(f => f.Status == FarmerStatus.Approved)
            .OrderBy(f => f.FarmName)
            .Select(f => new FarmerOption { Id = f.Id, Name = f.FarmName })
            .ToListAsync();
        return View(reviews);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitFarmer(int farmerId, int rating, string? title, string? comment, string? returnTo)
    {
        if (farmerId <= 0)
        {
            TempData["Error"] = "Please choose a farmer from the list.";
            return returnTo == "reviews" ? RedirectToAction(nameof(Index)) : await RedirectToFarmerAsync(farmerId);
        }

        if (rating is < 1 or > 5 || string.IsNullOrWhiteSpace(comment) || comment.Trim().Length > 2000)
        {
            TempData["Error"] = "Please provide a rating from 1 to 5 and a review message (maximum 2000 characters).";
            return returnTo == "reviews" ? RedirectToAction(nameof(Index)) : await RedirectToFarmerAsync(farmerId);
        }

        var farmerExists = await _unitOfWork.Repository<FarmerModel>().AnyAsync(f => f.Id == farmerId && f.Status == FarmerStatus.Approved);
        if (!farmerExists)
        {
            TempData["Error"] = "That farmer could not be found.";
            return returnTo == "reviews" ? RedirectToAction(nameof(Index)) : await RedirectToFarmerAsync(farmerId);
        }

        // Latest completed order with this farmer (optional - only used to link the review when one exists)
        var completedOrderId = await _unitOfWork.Repository<Order>().Query()
            .Where(o => o.CustomerId == CustomerId && o.FarmerId == farmerId && o.Status == OrderStatus.Completed)
            .OrderByDescending(o => o.CompletedAt)
            .Select(o => (int?)o.Id)
            .FirstOrDefaultAsync();

        var duplicate = await _unitOfWork.Repository<Review>().AnyAsync(r => r.CustomerId == CustomerId && r.FarmerId == farmerId && r.ProductId == null);
        if (duplicate)
        {
            TempData["Error"] = "You have already reviewed this farmer. Choose another farmer to review.";
            return returnTo == "reviews" ? RedirectToAction(nameof(Index)) : await RedirectToFarmerAsync(farmerId);
        }

        await _unitOfWork.Repository<Review>().AddAsync(new Review
        {
            CustomerId = CustomerId,
            FarmerId = farmerId,
            OrderId = completedOrderId,
            Rating = rating,
            Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
            Comment = comment.Trim(),
            IsApproved = true,
            CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = "Thank you. Your farmer review has been submitted.";
        return returnTo == "reviews" ? RedirectToAction(nameof(Index)) : await RedirectToFarmerAsync(farmerId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int productId, int? orderId, int rating, string? title, string? comment)
    {
        if (rating is < 1 or > 5)
        {
            TempData["Error"] = "Rating must be between 1 and 5.";
            return await RedirectToProductAsync(productId);
        }

        if (string.IsNullOrWhiteSpace(comment) || comment.Trim().Length > 2000)
        {
            TempData["Error"] = "Please provide a review comment (maximum 2000 characters).";
            return await RedirectToProductAsync(productId);
        }

        var purchaseQuery = _unitOfWork.Repository<Order>().Query()
            .Include(o => o.Items)
            .Where(o => o.Id == (orderId ?? 0)
                        && o.CustomerId == CustomerId
                        && o.Status == OrderStatus.Completed
                        && o.Items.Any(i => i.ProductId == productId));

        var completedOrder = orderId.HasValue
            ? await purchaseQuery.FirstOrDefaultAsync()
            : await _unitOfWork.Repository<Order>().Query()
                .Include(o => o.Items)
                .Where(o => o.CustomerId == CustomerId && o.Status == OrderStatus.Completed && o.Items.Any(i => i.ProductId == productId))
                .OrderByDescending(o => o.CompletedAt)
                .FirstOrDefaultAsync();

        if (completedOrder == null)
        {
            TempData["Error"] = "You can review this product only after completing a purchase.";
            return await RedirectToProductAsync(productId);
        }

        var product = await _unitOfWork.Repository<Product>().Query()
            .Include(p => p.Farmer)
            .FirstOrDefaultAsync(p => p.Id == productId && p.Farmer.Status == FarmerStatus.Approved);
        if (product == null) return NotFound();

        var duplicate = await _unitOfWork.Repository<Review>().AnyAsync(r =>
            r.CustomerId == CustomerId && r.ProductId == productId && r.OrderId == completedOrder.Id);
        if (duplicate)
        {
            TempData["Error"] = "You have already reviewed this purchase.";
            return await RedirectToProductAsync(productId);
        }

        await _unitOfWork.Repository<Review>().AddAsync(new Review
        {
            CustomerId = CustomerId,
            ProductId = productId,
            FarmerId = product.FarmerId,
            OrderId = completedOrder.Id,
            Rating = rating,
            Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
            Comment = comment.Trim(),
            IsApproved = true,
            CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "Thank you. Your review has been submitted.";
        return await RedirectToProductAsync(productId);
    }

    private async Task<IActionResult> RedirectToFarmerAsync(int farmerId)
    {
        var exists = await _unitOfWork.Repository<FarmerModel>().AnyAsync(f => f.Id == farmerId && f.Status == FarmerStatus.Approved);
        return exists ? RedirectToAction("Details", "Farmers", new { area = "", id = farmerId }) : RedirectToAction("Index", "Farmers", new { area = "" });
    }

    private async Task<IActionResult> RedirectToProductAsync(int productId)
    {
        var slug = await _unitOfWork.Repository<Product>().Query()
            .Where(p => p.Id == productId)
            .Select(p => p.Slug)
            .FirstOrDefaultAsync();
        return slug == null ? RedirectToAction("Index", "Products", new { area = "" })
            : RedirectToAction("Details", "Products", new { area = "", slug });
    }
}

public class FarmerOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ReviewInputModel
{
    [Range(1, 5)]
    public int Rating { get; set; }
    [StringLength(120)]
    public string? Title { get; set; }
    [Required, StringLength(2000)]
    public string Comment { get; set; } = string.Empty;
}
