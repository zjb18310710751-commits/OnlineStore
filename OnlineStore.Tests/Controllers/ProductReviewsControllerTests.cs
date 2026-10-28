using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Controllers;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.ViewModels;

namespace OnlineStore.Tests.Controllers;

public class ProductReviewsControllerTests
{
    [Fact]
    public async Task Details_ExposesOnlyFiveMostRecentReviews()
    {
        using var context = CreateContext();
        await SeedProductAndReviewsAsync(context);
        var result = Assert.IsType<ViewResult>(await CreateController(context).Details(1));

        var reviews = Assert.IsAssignableFrom<IReadOnlyList<Review>>(result.ViewData["LatestReviews"]);
        var summary = Assert.IsType<ReviewSummary>(result.ViewData["ReviewSummary"]);

        Assert.Equal(5, reviews.Count);
        Assert.Equal(8, summary.TotalCount);
        Assert.Equal(8, result.ViewData["ReviewCount"]);
        Assert.True(reviews.Zip(reviews.Skip(1)).All(pair => pair.First.CreatedAt >= pair.Second.CreatedAt));
    }

    [Fact]
    public async Task ReviewsArchive_ReturnsAllReviewsThroughPagedViewModel()
    {
        using var context = CreateContext();
        await SeedProductAndReviewsAsync(context);

        var result = Assert.IsType<ViewResult>(
            await CreateController(context).Reviews(1, page: 1, filter: "all", sort: "highest"));
        var model = Assert.IsType<ProductReviewsPageViewModel>(result.Model);

        Assert.Equal(8, model.TotalCount);
        Assert.Equal(8, model.Reviews.Count);
        Assert.Equal("all", model.Filter);
        Assert.Equal("highest", model.Sort);
        Assert.Equal(5, model.RatingCounts[5]);
    }

    [Fact]
    public async Task ReviewsArchive_AjaxRequestReturnsEmbeddedPartial()
    {
        using var context = CreateContext();
        await SeedProductAndReviewsAsync(context);
        var controller = CreateController(context);
        controller.HttpContext.Request.Headers.XRequestedWith = "XMLHttpRequest";

        var result = Assert.IsType<PartialViewResult>(
            await controller.Reviews(1, page: 2, filter: "photos", sort: "lowest"));
        var model = Assert.IsType<ProductReviewsPageViewModel>(result.Model);

        Assert.Equal("_ReviewArchive", result.ViewName);
        Assert.Equal(1, model.Page);
        Assert.Equal(4, model.TotalCount);
        Assert.Equal("photos", model.Filter);
        Assert.Equal("lowest", model.Sort);
    }

    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task SeedProductAndReviewsAsync(ApplicationDbContext context)
    {
        context.Shops.Add(new Shop { Id = 1, Name = "Store" });
        context.Products.Add(new Product
        {
            Id = 1,
            Name = "Massage Gun",
            NameEn = "Massage Gun",
            Slug = "massage-gun",
            Price = 100,
            Stock = 10,
            IsActive = true,
            ShopId = 1
        });
        var createdAt = DateTime.UtcNow.AddDays(-8);
        context.Reviews.AddRange(Enumerable.Range(1, 8).Select(index => new Review
        {
            ProductId = 1,
            UserId = $"buyer-{index}",
            UserName = $"Buyer {index}",
            OrderId = index,
            Rating = index <= 5 ? 5 : index - 4,
            Comment = $"Review {index}",
            ImageUrl = index % 2 == 0 ? $"/uploads/review-{index}.jpg" : null,
            CreatedAt = createdAt.AddDays(index)
        }));
        await context.SaveChangesAsync();
    }

    private static ProductsController CreateController(ApplicationDbContext context) =>
        new(new ProductService(context), null!, new ReviewService(context), new OrderService(context), null!)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}
