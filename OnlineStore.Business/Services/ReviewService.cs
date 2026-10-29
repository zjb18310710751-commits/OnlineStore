using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.ViewModels;

namespace OnlineStore.Services;

public class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _context;

    public ReviewService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<Review>> GetProductReviewsPagedAsync(
        int productId,
        int page,
        int pageSize,
        string filter,
        string sort)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _context.Reviews
            .AsNoTracking()
            .Where(review => review.ProductId == productId);
        if (string.Equals(filter, "photos", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(review => review.ImageUrl != null && review.ImageUrl != string.Empty);
        }

        query = sort.ToLowerInvariant() switch
        {
            "highest" => query.OrderByDescending(review => review.Rating)
                .ThenByDescending(review => review.CreatedAt)
                .ThenByDescending(review => review.Id),
            "lowest" => query.OrderBy(review => review.Rating)
                .ThenByDescending(review => review.CreatedAt)
                .ThenByDescending(review => review.Id),
            _ => query.OrderByDescending(review => review.CreatedAt)
                .ThenByDescending(review => review.Id)
        };

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        var currentPage = Math.Clamp(page, 1, totalPages);
        var reviews = await query
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return new PagedResult<Review>(reviews, currentPage, pageSize, total);
    }

    public async Task<ReviewSummary> GetProductSummaryAsync(int productId)
    {
        var reviews = _context.Reviews
            .AsNoTracking()
            .Where(review => review.ProductId == productId);
        var counts = await reviews
            .GroupBy(review => review.Rating)
            .Select(group => new { Rating = group.Key, Count = group.Count() })
            .ToListAsync();
        var aggregate = (await reviews
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                PhotoCount = group.Count(review => !string.IsNullOrWhiteSpace(review.ImageUrl)),
                Average = group.Average(review => (double)review.Rating)
            })
            .ToListAsync())
            .FirstOrDefault();
        var ratingCounts = Enumerable.Range(1, 5)
            .ToDictionary(star => star, star => counts.FirstOrDefault(item => item.Rating == star)?.Count ?? 0);
        return new ReviewSummary(
            aggregate?.Total ?? 0,
            aggregate?.PhotoCount ?? 0,
            aggregate is null ? 0 : Math.Round(aggregate.Average, 1),
            ratingCounts);
    }

    public async Task<PagedResult<Review>> GetPagedAsync(int page, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _context.Reviews
            .AsNoTracking()
            .Include(review => review.Product)
            .OrderByDescending(review => review.CreatedAt);
        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<Review>(items, page, pageSize, total);
    }

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

}
