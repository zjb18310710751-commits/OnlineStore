using OnlineStore.Models;

namespace OnlineStore.Services;

public interface IReviewService
{

    Task<IReadOnlyList<Review>> GetByProductIdAsync(int productId);

    Task<IReadOnlyList<Review>> GetAllAsync();

    Task<IReadOnlyList<Review>> GetByUserAsync(string userId);

    Task<Review> CreateAsync(Review review);

    Task DeleteAsync(int id);

    Task<int> CountAsync();
}
