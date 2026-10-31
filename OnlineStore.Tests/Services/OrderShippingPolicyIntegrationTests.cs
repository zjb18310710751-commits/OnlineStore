using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Tests.Services;

public class OrderShippingPolicyIntegrationTests
{
    [Fact]
    public async Task CreateAsync_UsesReplacementShippingPolicy_AndPersistsItsQuoteWithStockChange()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var product = new Product
        {
            Id = 1, Name = "Book", Price = 10m, Stock = 5, ShopId = 1,
            ShippingFrom = "Cambridge", ShippingFeeAmount = 3m
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        product.ShippingFrom = "Unsaved stale origin";

        using var services = new ServiceCollection()
            .AddSingleton<IOrderShippingPolicy, PriorityShippingPolicy>()
            .BuildServiceProvider();
        var service = ActivatorUtilities.CreateInstance<OrderService>(services, context);

        var order = await service.CreateAsync(new Order
        {
            OrderNo = "POLICY-REPLACEMENT", UserId = "buyer",
            Items = new List<OrderItem> { new() { ProductId = 1, Price = 10m, Quantity = 2 } }
        });
        context.ChangeTracker.Clear();
        var saved = await context.Orders.SingleAsync(candidate => candidate.Id == order.Id);

        Assert.Equal(6m, saved.ShippingAmount);
        Assert.Equal(26m, saved.Total);
        Assert.Equal("Priority: Cambridge", saved.ShippingOrigin);
        Assert.Equal("Priority warehouse", saved.ShippingOriginEn);
        Assert.Equal("优先配送", saved.ShippingFeeText);
        Assert.Equal("Priority shipping", saved.ShippingFeeTextEn);
        Assert.Equal("优先发货", saved.DispatchTimeText);
        Assert.Equal("Priority dispatch", saved.DispatchTimeTextEn);
        Assert.Equal(new DateTime(2030, 1, 2, 12, 0, 0, DateTimeKind.Utc), saved.ExpectedDispatchAt);
        Assert.Equal(OrderStatus.PendingShipment, saved.Status);
        Assert.Equal(3, (await context.Products.SingleAsync()).Stock);
    }

    private sealed class PriorityShippingPolicy : IOrderShippingPolicy
    {
        public OrderShippingQuote Quote(IReadOnlyList<OrderShippingProduct> products, bool freeShipping, DateTime utcNow) =>
            new($"Priority: {products.Single().ShippingFrom}", "Priority warehouse",
                "优先配送", "Priority shipping", "优先发货", "Priority dispatch",
                new DateTime(2030, 1, 2, 12, 0, 0, DateTimeKind.Utc),
                freeShipping ? 0 : products.Sum(product => product.ShippingFeeAmount) * 2);
    }
}
