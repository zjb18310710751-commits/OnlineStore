using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Tests.Services;

public class StorefrontHeaderServiceTests
{
    [Fact]
    public async Task GetAsync_GuestReturnsCategoriesAndZeroPersonalCounts()
    {
        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        context.Categories.Add(new Category { Name = "Books", DisplayOrder = 1 });
        await context.SaveChangesAsync();

        var service = new StorefrontHeaderService(
            new CategoryService(context),
            null!,
            null!,
            null!,
            null!);

        var header = await service.GetAsync(new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.Equal("Books", Assert.Single(header.Categories).Name);
        Assert.Null(header.CurrentUser);
        Assert.Equal(0, header.FavoriteCount);
        Assert.Equal(0, header.ActiveItemCount);
        Assert.Equal(0, header.AvailableCouponCount);
    }
}
