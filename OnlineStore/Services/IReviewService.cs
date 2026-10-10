using OnlineStore.Models;

namespace OnlineStore.Services;

/// <summary>商品评价业务抽象。</summary>
public interface IReviewService
{
    /// <summary>读取商品评价（按时间倒序）。</summary>
    Task<IReadOnlyList<Review>> GetByProductIdAsync(int productId);

    /// <summary>读取全部评价（含商品，按时间倒序），用于后台管理。</summary>
    Task<IReadOnlyList<Review>> GetAllAsync();

    Task<Review> CreateAsync(Review review);

    Task DeleteAsync(int id);

    Task<int> CountAsync();
}
