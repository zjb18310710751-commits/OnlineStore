using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Tests.Services;

public class CartServiceTests
{

    private sealed class FakeCartStore : ICartStore
    {
        public List<CartItem> Items { get; private set; } = new();

        public Task<List<CartItem>> GetAsync() => Task.FromResult(new List<CartItem>(Items));

        public Task SaveAsync(List<CartItem> items)
        {
            Items = items;
            return Task.CompletedTask;
        }
    }

    private static async Task<ApplicationDbContext> CreateSeededContextAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);

        context.Products.AddRange(
            new Product { Id = 1, Name = "无线耳机", Price = 299m, Stock = 5, IsActive = true },
            new Product { Id = 2, Name = "缺货商品", Price = 10m, Stock = 0, IsActive = true },
            new Product { Id = 3, Name = "已下架商品", Price = 10m, Stock = 5, IsActive = false });
        await context.SaveChangesAsync();

        return context;
    }

    [Fact]
    public async Task AddAsync_AddsNewItem_WithCorrectTotals()
    {
        using var context = await CreateSeededContextAsync();
        var cartService = new CartService(new FakeCartStore(), new ProductService(context), new ProductSkuService(context));

        await cartService.AddAsync(1, 2);

        var cart = await cartService.GetCartAsync();
        Assert.Single(cart.Items);
        Assert.Equal(1, cart.Items[0].ProductId);
        Assert.Equal(2, cart.Items[0].Quantity);
        Assert.Equal(598m, cart.Total);
        Assert.Equal(2, cart.TotalItems);
        Assert.Equal(1, cart.ItemCount);
    }

    [Fact]
    public async Task AddAsync_IncrementsExistingItem_AndRespectsStock()
    {
        using var context = await CreateSeededContextAsync();
        var cartService = new CartService(new FakeCartStore(), new ProductService(context), new ProductSkuService(context));

        await cartService.AddAsync(1, 3);
        await cartService.AddAsync(1, 3);

        var cart = await cartService.GetCartAsync();
        Assert.Single(cart.Items);
        Assert.Equal(5, cart.Items[0].Quantity);
    }

    [Fact]
    public async Task AddAsync_IgnoresOutOfStockOrInactiveProduct()
    {
        using var context = await CreateSeededContextAsync();
        var cartService = new CartService(new FakeCartStore(), new ProductService(context), new ProductSkuService(context));

        await cartService.AddAsync(2);
        await cartService.AddAsync(3);

        Assert.Empty((await cartService.GetCartAsync()).Items);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesQuantity()
    {
        using var context = await CreateSeededContextAsync();
        var cartService = new CartService(new FakeCartStore(), new ProductService(context), new ProductSkuService(context));

        await cartService.AddAsync(1, 1);
        await cartService.UpdateAsync(1, null, 4);

        var cart = await cartService.GetCartAsync();
        Assert.Equal(4, cart.Items[0].Quantity);
    }

    [Fact]
    public async Task UpdateAsync_RemovesItem_WhenQuantityIsZero()
    {
        using var context = await CreateSeededContextAsync();
        var cartService = new CartService(new FakeCartStore(), new ProductService(context), new ProductSkuService(context));

        await cartService.AddAsync(1, 1);
        await cartService.UpdateAsync(1, null, 0);

        Assert.Empty((await cartService.GetCartAsync()).Items);
    }

    [Fact]
    public async Task RemoveAsync_RemovesItem()
    {
        using var context = await CreateSeededContextAsync();
        var cartService = new CartService(new FakeCartStore(), new ProductService(context), new ProductSkuService(context));

        await cartService.AddAsync(1, 1);
        await cartService.RemoveAsync(1, null);

        Assert.Empty((await cartService.GetCartAsync()).Items);
    }

    [Fact]
    public async Task ClearAsync_EmptiesCart()
    {
        using var context = await CreateSeededContextAsync();
        var cartService = new CartService(new FakeCartStore(), new ProductService(context), new ProductSkuService(context));

        await cartService.AddAsync(1, 2);
        await cartService.ClearAsync();

        Assert.Empty((await cartService.GetCartAsync()).Items);
    }
}
