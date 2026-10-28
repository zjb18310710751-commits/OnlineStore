using OnlineStore.Models;
using OnlineStore.ViewModels;

namespace OnlineStore.Services;

public interface IReviewService
{

    Task<IReadOnlyList<Review>> GetByProductIdAsync(int productId);

    Task<PagedResult<Review>> GetProductReviewsPagedAsync(
        int productId,
        int page,
        int pageSize,
        string filter,
        string sort);

    Task<ReviewSummary> GetProductSummaryAsync(int productId);

    Task<IReadOnlyList<Review>> GetAllAsync();
    Task<PagedResult<Review>> GetPagedAsync(int page, int pageSize);

    Task<IReadOnlyList<Review>> GetByUserAsync(string userId);

    Task<Review> CreateAsync(Review review);

    Task DeleteAsync(int id);

    Task<int> CountAsync();
}

public sealed record ReviewSummary(
    int TotalCount,
    int PhotoCount,
    double Average,
    IReadOnlyDictionary<int, int> RatingCounts);
