using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Tests.Services;

public class HeroSlideServiceTests
{
    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static HeroSlide Slide(int id, int order, bool active = true) => new()
    {
        Id = id,
        Title = $"Collection {id}",
        Heading = $"Headline {id}",
        ImageUrl = $"/images/slide-{id}.png",
        LinkUrl = "/Products",
        DisplayOrder = order,
        IsActive = active
    };

    [Fact]
    public async Task GetAllAsync_SortsByDisplayOrderThenId_AndKeepsHiddenSlidesInAdminList()
    {
        using var context = CreateContext();
        context.HeroSlides.AddRange(Slide(3, 10), Slide(2, 5, false), Slide(1, 5));
        await context.SaveChangesAsync();
        var service = new HeroSlideService(context);

        var slides = await service.GetAllAsync();

        Assert.Equal(new[] { 1, 2, 3 }, slides.Select(slide => slide.Id));
        Assert.False(slides[1].IsActive);
    }

    [Fact]
    public async Task GetAllAsync_ActiveOnlyExcludesHiddenSlides_AndPreservesOrder()
    {
        using var context = CreateContext();
        context.HeroSlides.AddRange(Slide(3, 1), Slide(2, 0, false), Slide(1, 2));
        await context.SaveChangesAsync();
        var service = new HeroSlideService(context);

        var slides = await service.GetAllAsync(activeOnly: true);

        Assert.Equal(new[] { 3, 1 }, slides.Select(slide => slide.Id));
    }

    [Fact]
    public async Task CreateAndUpdateAsync_PersistContentAndVisibilityChanges()
    {
        using var context = CreateContext();
        var service = new HeroSlideService(context);
        var original = Slide(0, 3);
        original.TitleEn = "Summer collection";
        original.DescriptionEn = "Made for sunny days.";

        var created = await service.CreateAsync(original);
        Assert.True(created.Id > 0);
        context.ChangeTracker.Clear();
        var saved = await service.GetByIdAsync(created.Id);
        Assert.NotNull(saved);
        Assert.Equal("Summer collection", saved.TitleEn);
        Assert.Equal("Made for sunny days.", saved.DescriptionEn);

        saved.Heading = "New headline";
        saved.LinkUrl = "/Products?categoryId=2";
        saved.DisplayOrder = 0;
        saved.IsActive = false;
        await service.UpdateAsync(saved);
        context.ChangeTracker.Clear();

        var updated = await context.HeroSlides.SingleAsync();
        Assert.Equal("New headline", updated.Heading);
        Assert.Equal("/Products?categoryId=2", updated.LinkUrl);
        Assert.Equal(0, updated.DisplayOrder);
        Assert.Empty(await service.GetAllAsync(activeOnly: true));
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyRequestedSlide_AndReportsMissingId()
    {
        using var context = CreateContext();
        context.HeroSlides.AddRange(Slide(1, 1), Slide(2, 2));
        await context.SaveChangesAsync();
        var service = new HeroSlideService(context);

        Assert.True(await service.DeleteAsync(1));
        Assert.False(await service.DeleteAsync(1));
        Assert.Null(await service.GetByIdAsync(1));
        Assert.Equal(2, Assert.Single(await service.GetAllAsync()).Id);
    }

    [Fact]
    public async Task EmptyCarousel_RemainsEmpty_AfterAllSlidesAreDeleted()
    {
        using var context = CreateContext();
        var service = new HeroSlideService(context);
        var created = await service.CreateAsync(Slide(0, 1));

        await service.DeleteAsync(created.Id);

        Assert.Empty(await service.GetAllAsync());
        Assert.Empty(await service.GetAllAsync(activeOnly: true));
    }
}
