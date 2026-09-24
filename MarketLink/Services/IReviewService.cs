using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Services
{
    public interface IReviewService
    {
        Task<ReviewIndexViewModel> GetReviewsAsync(ReviewFilterViewModel filter);
        Task<ReviewDetailsViewModel?> GetReviewDetailsAsync(int reviewId);
        Task<bool> SetRemovedAsync(int reviewId, bool isRemoved);
    }
}
