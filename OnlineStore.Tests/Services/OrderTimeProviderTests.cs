using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Tests.Services;

public class OrderTimeProviderTests
{
    [Fact]
    public async Task CreateOrder_UsesInjectedClock()
    {
        var now = new DateTimeOffset(2026, 10, 29, 9, 30, 0, TimeSpan.Zero);
        await using var context = await CreateContextAsync();
        var service = new OrderService(
            context,
            timeProvider: new FixedTimeProvider(now));
        var order = new Order
        {
            OrderNo = "TEST-ORDER",
            UserId = "user",
            Items = [new OrderItem { ProductId = 1, Price = 10m, Quantity = 1 }]
        };

        await service.CreateAsync(order);

        Assert.Equal(now.UtcDateTime, order.CreatedAt);
        Assert.Equal(now.AddHours(72).UtcDateTime, order.ExpectedDispatchAt);
    }

    [Fact]
    public void OrderNumberGenerator_UsesInjectedClock()
    {
        var generator = new OrderNumberGenerator(
            new FixedTimeProvider(new DateTimeOffset(2026, 10, 29, 9, 30, 0, TimeSpan.Zero)));

        var orderNumber = generator.Generate();

        Assert.StartsWith("OS20261029093000", orderNumber);
        Assert.Matches(@"^OS\d{14}[A-F0-9]{8}$", orderNumber);
    }

    private static async Task<ApplicationDbContext> CreateContextAsync()
    {
        var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        context.Shops.Add(new Shop { Id = 1, Name = "Shop" });
        context.Products.Add(new Product
        {
            Id = 1,
            Name = "Product",
            Price = 10m,
            Stock = 5,
            ShopId = 1
        });
        await context.SaveChangesAsync();
        return context;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
