using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Tests.Services;

public class ReviewOrderTests
{
    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task GetByUserAsync_ReturnsOnlyCurrentUsersReviews()
    {
        using var context = CreateContext();
        context.Reviews.AddRange(
            new Review { ProductId = 1, UserId = "user-1", UserName = "Buyer", OrderId = 10, Rating = 4 },
            new Review { ProductId = 2, UserId = "user-2", UserName = "Other", OrderId = 11, Rating = 5 });
        await context.SaveChangesAsync();

        var reviews = await new ReviewService(context).GetByUserAsync("user-1");

        var review = Assert.Single(reviews);
        Assert.Equal(10, review.OrderId);
    }

    [Fact]
    public async Task CreateAsync_SameOrderItem_UpdatesExistingReview()
    {
        using var context = CreateContext();
        var service = new ReviewService(context);
        await service.CreateAsync(new Review
        {
            ProductId = 1,
            UserId = "user-1",
            UserName = "Buyer",
            OrderId = 10,
            Rating = 3,
            Comment = "First"
        });
        await service.CreateAsync(new Review
        {
            ProductId = 1,
            UserId = "user-1",
            UserName = "Buyer",
            OrderId = 10,
            Rating = 5,
            Comment = "Updated"
        });

        var review = Assert.Single(await context.Reviews.ToListAsync());
        Assert.Equal(5, review.Rating);
        Assert.Equal("Updated", review.Comment);
    }

    [Fact]
    public async Task GetProductReviewsPagedAsync_FiltersPhotoReviews()
    {
        using var context = CreateContext();
        var createdAt = DateTime.UtcNow.AddDays(-12);
        context.Reviews.AddRange(Enumerable.Range(1, 12).Select(index => new Review
        {
            ProductId = 1,
            UserId = $"user-{index}",
            UserName = $"Buyer {index}",
            OrderId = index,
            Rating = index % 5 + 1,
            ImageUrl = index % 2 == 0 ? $"/uploads/review-{index}.jpg" : null,
            CreatedAt = createdAt.AddDays(index)
        }));
        await context.SaveChangesAsync();

        var result = await new ReviewService(context)
            .GetProductReviewsPagedAsync(1, 1, 10, "photos", "newest");

        Assert.Equal(6, result.TotalCount);
        Assert.Equal(6, result.Items.Count);
        Assert.All(result.Items, review => Assert.False(string.IsNullOrWhiteSpace(review.ImageUrl)));
    }

    [Fact]
    public async Task GetProductReviewsPagedAsync_SortsAndPaginatesWithoutLosingTotals()
    {
        using var context = CreateContext();
        var createdAt = DateTime.UtcNow.AddDays(-12);
        context.Reviews.AddRange(Enumerable.Range(1, 12).Select(index => new Review
        {
            ProductId = 1,
            UserId = $"user-{index}",
            UserName = $"Buyer {index}",
            OrderId = index,
            Rating = index % 5 + 1,
            CreatedAt = createdAt.AddDays(index)
        }));
        await context.SaveChangesAsync();

        var result = await new ReviewService(context)
            .GetProductReviewsPagedAsync(1, 2, 5, "all", "highest");

        Assert.Equal(12, result.TotalCount);
        Assert.Equal(5, result.Items.Count);
        Assert.Equal(2, result.Page);
        Assert.True(result.Items.Zip(result.Items.Skip(1)).All(pair => pair.First.Rating >= pair.Second.Rating));
    }

    [Fact]
    public async Task GetProductSummaryAsync_CountsAllReviewsAndPhotos()
    {
        using var context = CreateContext();
        context.Reviews.AddRange(
            new Review { ProductId = 1, UserName = "One", Rating = 5, ImageUrl = "/one.jpg" },
            new Review { ProductId = 1, UserName = "Two", Rating = 3 },
            new Review { ProductId = 2, UserName = "Other", Rating = 1, ImageUrl = "/other.jpg" });
        await context.SaveChangesAsync();

        var summary = await new ReviewService(context).GetProductSummaryAsync(1);

        Assert.Equal(2, summary.TotalCount);
        Assert.Equal(1, summary.PhotoCount);
        Assert.Equal(4.0, summary.Average);
        Assert.Equal(1, summary.RatingCounts[5]);
        Assert.Equal(1, summary.RatingCounts[3]);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsTenAdminRowsAndKeepsTotal()
    {
        using var context = CreateContext();
        context.Shops.Add(new Shop { Id = 1, Name = "Store" });
        context.Products.Add(new Product { Id = 1, Name = "Product", ShopId = 1 });
        context.Reviews.AddRange(Enumerable.Range(1, 12).Select(index => new Review
        {
            ProductId = 1,
            UserName = $"Buyer {index}",
            Rating = 5,
            CreatedAt = DateTime.UtcNow.AddMinutes(index)
        }));
        await context.SaveChangesAsync();

        var result = await new ReviewService(context).GetPagedAsync(1, 10);

        Assert.Equal(10, result.Items.Count);
        Assert.Equal(12, result.TotalCount);
        Assert.True(result.ShowPagination);
    }
}
