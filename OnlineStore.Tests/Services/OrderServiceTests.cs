using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Tests.Services;

public class OrderServiceTests
{
    private static async Task<(ApplicationDbContext Context, Order Order, Product Product)> SeedAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);

        var product = new Product { Id = 1, Name = "无线耳机", Price = 299m, Stock = 5, IsActive = true, ShopId = 1 };
        context.Products.Add(product);

        var order = new Order
        {
            Id = 1,
            OrderNo = "OS001",
            UserId = "user1",
            Total = 299m,
            Status = OrderStatus.PendingShipment,
            Items = new List<OrderItem>
            {
                new OrderItem { ProductId = 1, ProductName = "无线耳机", Price = 299m, Quantity = 2 }
            }
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        return (context, order, product);
    }

    [Fact]
    public async Task CancelAsync_PendingShipment_CancelsAndRestoresStock()
    {
        var (context, order, product) = await SeedAsync();
        var service = new OrderService(context);

        var ok = await service.CancelAsync(order.Id);

        Assert.True(ok);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(7, product.Stock);
    }

    [Fact]
    public async Task RefundAsync_Shipped_RefundsAndRestoresStock()
    {
        var (context, order, product) = await SeedAsync();
        order.Status = OrderStatus.Shipped;
        await context.SaveChangesAsync();
        var service = new OrderService(context);

        var ok = await service.RefundAsync(order.Id);

        Assert.True(ok);
        Assert.Equal(OrderStatus.Refunded, order.Status);
        Assert.Equal(7, product.Stock);
    }

    [Fact]
    public async Task ShipAsync_ThenCompleteAsync_TransitionsCorrectly()
    {
        var (context, order, _) = await SeedAsync();
        var service = new OrderService(context);

        Assert.True(await service.ShipAsync(order.Id));
        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.True(await service.CompleteAsync(order.Id));
        Assert.Equal(OrderStatus.Completed, order.Status);
    }

    [Fact]
    public async Task CancelAsync_NonPending_ReturnsFalse()
    {
        var (context, order, _) = await SeedAsync();
        order.Status = OrderStatus.Completed;
        await context.SaveChangesAsync();
        var service = new OrderService(context);

        Assert.False(await service.CancelAsync(order.Id));
    }

    [Fact]
    public async Task RefundAsync_PendingShipment_ReturnsFalse()
    {
        var (context, order, _) = await SeedAsync();
        var service = new OrderService(context);

        Assert.False(await service.RefundAsync(order.Id));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllOrders()
    {
        var (context, _, _) = await SeedAsync();
        context.Orders.Add(new Order { OrderNo = "OS002", Total = 100m, Status = OrderStatus.PendingShipment });
        await context.SaveChangesAsync();
        var service = new OrderService(context);

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task CreateAsync_DecrementsStock()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationDbContext(options);
        context.Products.Add(new Product { Id = 1, Name = "无线耳机", Price = 299m, Stock = 10, IsActive = true, ShopId = 1 });
        await context.SaveChangesAsync();

        var service = new OrderService(context);
        var order = new Order
        {
            OrderNo = "OS001",
            UserId = "u1",
            Total = 598m,
            Status = OrderStatus.PendingShipment,
            Items = new List<OrderItem>
            {
                new OrderItem { ProductId = 1, ProductName = "无线耳机", Price = 299m, Quantity = 2 }
            }
        };

        await service.CreateAsync(order);

        var product = await context.Products.FindAsync(1);
        Assert.Equal(8, product!.Stock);
    }
}
