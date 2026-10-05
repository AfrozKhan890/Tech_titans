using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MarketLink.Repositories;
using MarketLink.Services;
using MarketLink.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MarketLink.Controllers.Api;

[ApiController]
[Route("api/reviews")]
[Produces("application/json")]
public class ReviewsApiController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ReviewsApiController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("product/{productId:int}")]
    public async Task<IActionResult> GetProductReviews(int productId)
    {
        var reviews = await _unitOfWork.Repository<Review>().Query()
            .Include(r => r.Customer).ThenInclude(c => c.User)
            .Where(r => r.ProductId == productId && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.Rating,
                r.Title,
                r.Comment,
                r.FarmerResponse,
                r.CreatedAt,
                CustomerName = $"{r.Customer.User.FirstName} {r.Customer.User.LastName}".Trim()
            })
            .ToListAsync();

        return Ok(new { success = true, count = reviews.Count, reviews });
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReview([FromBody] ApiSubmitReviewRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, errors = ModelState });

        var customerId = await MarketLink.Services.ActorResolver.ResolveCustomerIdAsync(User, _unitOfWork, HttpContext);
        if (customerId == null) return NotFound(new { success = false, message = "Customer profile not found for this account." });
        if (request.Rating is < 1 or > 5 || string.IsNullOrWhiteSpace(request.Comment) || request.Comment.Trim().Length > 2000)
            return BadRequest(new { success = false, message = "Rating must be 1-5 and comment is required (maximum 2000 characters)." });

        if (request.ProductId is null && request.FarmerId is null)
            return BadRequest(new { success = false, message = "A review must target a produce or a farm." });

        Product? product = null;
        if (request.ProductId.HasValue)
        {
            product = await _unitOfWork.Repository<Product>().Query()
                .Include(p => p.Farmer)
                .FirstOrDefaultAsync(p => p.Id == request.ProductId.Value && p.Farmer.Status == MarketLink.Models.Enums.FarmerStatus.Approved);
            if (product == null) return NotFound(new { success = false, message = "That produce is no longer listed." });
            if (request.FarmerId.HasValue && request.FarmerId.Value != product.FarmerId)
                return BadRequest(new { success = false, message = "The selected farm does not own that produce." });
        }

        var farmerId = request.FarmerId ?? product!.FarmerId;
        if (!await _unitOfWork.Repository<Farmer>().AnyAsync(f => f.Id == farmerId && f.Status == MarketLink.Models.Enums.FarmerStatus.Approved))
            return NotFound(new { success = false, message = "That farm does not exist." });

        var completedOrderQuery = _unitOfWork.Repository<Order>().Query()
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId.Value && o.FarmerId == farmerId && o.Status == MarketLink.Models.Enums.OrderStatus.Completed);
        if (product != null)
            completedOrderQuery = completedOrderQuery.Where(o => o.Items.Any(i => i.ProductId == product.Id));

        var completedOrder = await completedOrderQuery.OrderByDescending(o => o.CompletedAt).FirstOrDefaultAsync();
        if (completedOrder == null)
            return Forbid();

        var duplicate = await _unitOfWork.Repository<Review>().AnyAsync(r =>
            r.CustomerId == customerId.Value &&
            r.OrderId == completedOrder.Id &&
            r.ProductId == (product == null ? (int?)null : product.Id) &&
            r.FarmerId == farmerId);
        if (duplicate) return Conflict(new { success = false, message = "You have already reviewed this completed purchase." });

        var review = new Review
        {
            CustomerId = customerId.Value,
            ProductId = product?.Id,
            FarmerId = farmerId,
            OrderId = completedOrder.Id,
            Rating = request.Rating,
            Title = request.Title?.Trim(),
            Comment = request.Comment.Trim(),
            IsApproved = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Review>().AddAsync(review);
        await _unitOfWork.SaveChangesAsync();
        return StatusCode(201, new { success = true, message = "Review submitted and awaiting moderation.", reviewId = review.Id });
    }
}

public class ApiSubmitReviewRequest
{
    public int? ProductId { get; set; }
    public int? FarmerId { get; set; }
    [Range(1, 5)]
    public int Rating { get; set; }
    public string? Title { get; set; }
    [Required]
    public string Comment { get; set; } = string.Empty;
}
