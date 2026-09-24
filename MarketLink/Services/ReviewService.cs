using Microsoft.EntityFrameworkCore;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Services
{
    public class ReviewService : IReviewService
    {
        private readonly MarketLinkDbContext _context;

        public ReviewService(MarketLinkDbContext context)
        {
            _context = context;
        }

        public async Task<ReviewIndexViewModel> GetReviewsAsync(ReviewFilterViewModel filter)
        {
            var query = _context.Reviews
                .Include(r => r.Customer)
                .Include(r => r.Product)
                .Include(r => r.Farmer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(r =>
                    (r.Customer != null && r.Customer.FullName.ToLower().Contains(term)) ||
                    (r.Farmer != null && r.Farmer.StallName.ToLower().Contains(term)) ||
                    (r.Product != null && r.Product.Name.ToLower().Contains(term)) ||
                    (r.Comment != null && r.Comment.ToLower().Contains(term)));
            }

            if (filter.Rating.HasValue)
            {
                query = query.Where(r => r.Rating == filter.Rating.Value);
            }

            if (filter.FarmerId.HasValue)
            {
                query = query.Where(r => r.FarmerId == filter.FarmerId.Value);
            }

            if (filter.IsRemoved.HasValue)
            {
                query = query.Where(r => r.IsRemoved == filter.IsRemoved.Value);
            }

            var totalCount = await query.CountAsync();

            var pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;
            var pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;

            var items = await query
                .OrderByDescending(r => r.ReviewDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new ReviewIndexViewModel
            {
                Filter = filter,
                Result = new PagedResult<Review>
                {
                    Items = items,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount
                },
                Farmers = await _context.Farmers.OrderBy(f => f.StallName).ToListAsync(),
                TotalReviews = await _context.Reviews.CountAsync(),
                VisibleCount = await _context.Reviews.CountAsync(r => !r.IsRemoved),
                RemovedCount = await _context.Reviews.CountAsync(r => r.IsRemoved),
                AverageRating = await _context.Reviews.AnyAsync(r => !r.IsRemoved)
                    ? await _context.Reviews.Where(r => !r.IsRemoved).AverageAsync(r => r.Rating)
                    : 0
            };

            return vm;
        }

        public async Task<ReviewDetailsViewModel?> GetReviewDetailsAsync(int reviewId)
        {
            var review = await _context.Reviews
                .Include(r => r.Customer)
                .Include(r => r.Product)
                .Include(r => r.Farmer)
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

            if (review == null)
            {
                return null;
            }

            return new ReviewDetailsViewModel { Review = review };
        }

        public async Task<bool> SetRemovedAsync(int reviewId, bool isRemoved)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null)
            {
                return false;
            }

            review.IsRemoved = isRemoved;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
