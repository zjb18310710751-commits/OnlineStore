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
}
