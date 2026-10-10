using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Tests.Services;

/// <summary>
/// 购物车业务层单元测试：验证加入、累加、库存上限、更新数量、移除与清空。
/// 通过 <see cref="ICartStore"/> 抽象隔离存储，用内存假实现替代 Session（可测试性）。
/// </summary>
public class CartServiceTests
{
    /// <summary>内存版购物车存储：替代真实的 Session 存储。</summary>
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
        var cartService = new CartService(new FakeCartStore(), new ProductService(context));

        await cartService.AddAsync(1, 2);

        var cart = await cartService.GetCartAsync();
        Assert.Single(cart.Items);
        Assert.Equal(1, cart.Items[0].ProductId);
        Assert.Equal(2, cart.Items[0].Quantity);
        Assert.Equal(598m, cart.Total);
        Assert.Equal(2, cart.TotalItems);
    }

    [Fact]
    public async Task AddAsync_IncrementsExistingItem_AndRespectsStock()
    {
        using var context = await CreateSeededContextAsync();
        var cartService = new CartService(new FakeCartStore(), new ProductService(context));

        await cartService.AddAsync(1, 3); // 库存 5，加 3
        await cartService.AddAsync(1, 3); // 再累加应被库存上限截断

        var cart = await cartService.GetCartAsync();
        Assert.Single(cart.Items);
        Assert.Equal(5, cart.Items[0].Quantity);
    }

    [Fact]
    public async Task AddAsync_IgnoresOutOfStockOrInactiveProduct()
    {
        using var context = await CreateSeededContextAsync();
        var cartService = new CartService(new FakeCartStore(), new ProductService(context));

        await cartService.AddAsync(2); // 缺货
        await cartService.AddAsync(3); // 已下架

        Assert.Empty((await cartService.GetCartAsync()).Items);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesQuantity()
    {
        using var context = await CreateSeededContextAsync();
        var cartService = new CartService(new FakeCartStore(), new ProductService(context));

        await cartService.AddAsync(1, 1);
        await cartService.UpdateAsync(1, 4);

        var cart = await cartService.GetCartAsync();
        Assert.Equal(4, cart.Items[0].Quantity);
    }

    [Fact]
    public async Task UpdateAsync_RemovesItem_WhenQuantityIsZero()
    {
        using var context = await CreateSeededContextAsync();
        var cartService = new CartService(new FakeCartStore(), new ProductService(context));

        await cartService.AddAsync(1, 1);
        await cartService.UpdateAsync(1, 0);

        Assert.Empty((await cartService.GetCartAsync()).Items);
    }

    [Fact]
    public async Task RemoveAsync_RemovesItem()
    {
        using var context = await CreateSeededContextAsync();
        var cartService = new CartService(new FakeCartStore(), new ProductService(context));

        await cartService.AddAsync(1, 1);
        await cartService.RemoveAsync(1);

        Assert.Empty((await cartService.GetCartAsync()).Items);
    }

    [Fact]
    public async Task ClearAsync_EmptiesCart()
    {
        using var context = await CreateSeededContextAsync();
        var cartService = new CartService(new FakeCartStore(), new ProductService(context));

        await cartService.AddAsync(1, 2);
        await cartService.ClearAsync();

        Assert.Empty((await cartService.GetCartAsync()).Items);
    }
}
