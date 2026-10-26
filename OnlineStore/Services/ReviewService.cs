using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;

namespace OnlineStore.Services;

public class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _context;

    public ReviewService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Review>> GetByProductIdAsync(int productId) =>
        await _context.Reviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<Review>> GetAllAsync() =>
        await _context.Reviews
            .AsNoTracking()
            .Include(r => r.Product)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<Review>> GetByUserAsync(string userId) =>
        await _context.Reviews
            .AsNoTracking()
            .Where(review => review.UserId == userId)
            .OrderByDescending(review => review.CreatedAt)
            .ToListAsync();

    public async Task<Review> CreateAsync(Review review)
    {
        review.CreatedAt = DateTime.UtcNow;
        if (review.OrderId.HasValue && !string.IsNullOrWhiteSpace(review.UserId))
        {
            var existing = await _context.Reviews.FirstOrDefaultAsync(item =>
                item.OrderId == review.OrderId
                && item.ProductId == review.ProductId
                && item.UserId == review.UserId);
            if (existing is not null)
            {
                existing.Rating = review.Rating;
                existing.Comment = review.Comment;
                existing.ImageUrl = review.ImageUrl ?? existing.ImageUrl;
                existing.CreatedAt = review.CreatedAt;
                await _context.SaveChangesAsync();
                return existing;
            }
        }

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        return review;
    }

    public async Task DeleteAsync(int id)
    {
        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id);
        if (review is not null)
        {
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
        }
    }

    public Task<int> CountAsync() => _context.Reviews.CountAsync();
}
