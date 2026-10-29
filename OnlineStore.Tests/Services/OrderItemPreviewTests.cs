using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.ViewModels;

namespace OnlineStore.Tests.Services;

public class OrderItemPreviewTests
{
    [Fact]
    public async Task Preview_UsesThePurchasedSkuImageWithoutChangingTheOrderSnapshot()
    {
        using var context = CreateContext();
        context.Products.Add(Product(1, "/main.png"));
        context.ProductSkus.Add(new ProductSku { Id = 4, ProductId = 1, Color = "Blue", Spec = "Large", ImageUrl = "/blue-large.png" });
        await context.SaveChangesAsync();
        var item = new OrderItem { Id = 11, ProductId = 1, SkuId = 4, ProductName = "Purchased name", Price = 19m, Quantity = 3 };

        var previews = await GetPreviewsAsync(new OrderService(context), item);

        Assert.Equal("/blue-large.png", previews[11].ImageUrl);
        Assert.True(previews[11].CanViewProduct);
        Assert.Equal("Purchased name", item.ProductName);
        Assert.Equal(19m, item.Price);
        Assert.Equal(3, item.Quantity);
    }

    [Fact]
    public async Task Preview_DoesNotUseASkuImageFromAnotherProduct()
    {
        using var context = CreateContext();
        context.Products.AddRange(Product(1, "/first.png"), Product(2, "/second.png"));
        context.ProductSkus.Add(new ProductSku { Id = 9, ProductId = 2, Color = "Red", Spec = "Small", ImageUrl = "/wrong-sku.png" });
        await context.SaveChangesAsync();

        var previews = await GetPreviewsAsync(new OrderService(context), new OrderItem { Id = 12, ProductId = 1, SkuId = 9 });

        Assert.Equal("/first.png", previews[12].ImageUrl);
    }

    [Fact]
    public async Task Preview_FallsBackToTheMainImageWhenTheSkuHasNoImage()
    {
        using var context = CreateContext();
        context.Products.Add(Product(1, "/main.png"));
        context.ProductSkus.Add(new ProductSku { Id = 3, ProductId = 1, Color = "White", Spec = "M", ImageUrl = " " });
        await context.SaveChangesAsync();

        var previews = await GetPreviewsAsync(new OrderService(context), new OrderItem { Id = 13, ProductId = 1, SkuId = 3 });

        Assert.Equal("/main.png", previews[13].ImageUrl);
    }

    [Fact]
    public async Task Preview_UsesTheFirstUsableGalleryImageWhenTheMainImageIsMissing()
    {
        using var context = CreateContext();
        context.Products.Add(Product(1, null));
        context.ProductImages.AddRange(
            new ProductImage { Id = 1, ProductId = 1, ImageUrl = " ", SortOrder = 0 },
            new ProductImage { Id = 2, ProductId = 1, ImageUrl = "/later.png", SortOrder = 2 },
            new ProductImage { Id = 3, ProductId = 1, ImageUrl = "/first.png", SortOrder = 1 });
        await context.SaveChangesAsync();

        var previews = await GetPreviewsAsync(new OrderService(context), new OrderItem { Id = 14, ProductId = 1 });

        Assert.Equal("/first.png", previews[14].ImageUrl);
    }

    [Fact]
    public async Task Preview_PreservesUnavailableAndMissingItemsForTheOrderView()
    {
        using var context = CreateContext();
        context.Products.AddRange(Product(1, "/inactive.png", false), Product(2, null));
        await context.SaveChangesAsync();
        var items = new[]
        {
            new OrderItem { Id = 15, ProductId = 1 },
            new OrderItem { Id = 16, ProductId = 99 },
            new OrderItem { Id = 17, ProductId = 2 }
        };

        var previews = await GetPreviewsAsync(new OrderService(context), items);

        Assert.Equal(3, previews.Count);
        Assert.Equal("/inactive.png", previews[15].ImageUrl);
        Assert.False(previews[15].CanViewProduct);
        Assert.Null(previews[16].ImageUrl);
        Assert.False(previews[16].CanViewProduct);
        Assert.Null(previews[17].ImageUrl);
        Assert.True(previews[17].CanViewProduct);
    }

    [Fact]
    public async Task Preview_ReturnsAnEmptyMapForNoOrderItems()
    {
        using var context = CreateContext();

        var previews = await GetPreviewsAsync(new OrderService(context));

        Assert.Empty(previews);
    }

    private static Product Product(int id, string? image, bool active = true) =>
        new() { Id = id, Name = "Current catalog name", ImageUrl = image, ShopId = 1, IsActive = active };

    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Task<IReadOnlyDictionary<int, OrderItemPreviewViewModel>> GetPreviewsAsync(OrderService service, params OrderItem[] items) =>
        service.GetOrderItemPreviewsAsync(items);
}
