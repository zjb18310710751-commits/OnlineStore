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
    public async Task ShipAsync_DoesNotCompleteOrderAndCreatesNoConfirmationDeadline()
    {
        var (context, order, _) = await SeedAsync();
        var service = new OrderService(context);

        Assert.True(await service.ShipAsync(order.Id, "SF Express", "TEST00000001"));
        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.Null(order.AutoConfirmAt);
        Assert.Null(order.SignedAt);
    }

    [Fact]
    public async Task ConfirmReceipt_RequiresDeliveredAndCompletesManually()
    {
        var (context, order, _) = await SeedAsync();
        var service = new OrderService(context);
        Assert.True(await service.ShipAsync(order.Id, "SF Express", "TEST00000001"));

        Assert.False(await service.ConfirmReceiptAsync(order.Id, order.UserId!));
        Assert.Equal(OrderStatus.Shipped, order.Status);

        order.ShippedAt = DateTime.UtcNow.AddSeconds(-10);
        await context.SaveChangesAsync();
        await service.GetTrackingAsync(order.Id);
        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.NotNull(order.AutoConfirmAt);

        Assert.True(await service.ConfirmReceiptAsync(order.Id, order.UserId!));
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.NotNull(order.SignedAt);
        Assert.False(order.ReceiptConfirmedAutomatically);
    }

    [Fact]
    public async Task ProcessPendingOrderStates_AutoConfirmsAfterSevenDayWindow()
    {
        var (context, order, _) = await SeedAsync();
        var deliveredAt = DateTime.UtcNow.AddDays(-8);
        var autoConfirmAt = deliveredAt.AddDays(7);
        order.Status = OrderStatus.Delivered;
        order.ShippedAt = deliveredAt.AddSeconds(-9);
        order.DeliveredAt = deliveredAt;
        order.AutoConfirmAt = autoConfirmAt;
        await context.SaveChangesAsync();
        var service = new OrderService(context);

        await service.ProcessPendingOrderStatesAsync();

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(autoConfirmAt, order.SignedAt);
        Assert.True(order.ReceiptConfirmedAutomatically);
    }

    [Fact]
    public async Task ReportReceiptException_RecordsAuditPausesAutoConfirmAndAllowsRefund()
    {
        var (context, order, product) = await SeedAsync();
        order.Status = OrderStatus.Delivered;
        order.DeliveredAt = DateTime.UtcNow.AddDays(-1);
        order.AutoConfirmAt = DateTime.UtcNow.AddDays(-1);
        await context.SaveChangesAsync();
        var service = new OrderService(context);

        Assert.True(await service.ReportReceiptExceptionAsync(order.Id, "Buyer did not receive the parcel.", "admin@store.com"));
        await service.ProcessPendingOrderStatesAsync();

        Assert.Equal(OrderStatus.ReceiptException, order.Status);
        Assert.Equal("Buyer did not receive the parcel.", order.ReceiptExceptionReason);
        Assert.Equal("admin@store.com", order.ReceiptExceptionReportedBy);
        Assert.NotNull(order.ReceiptExceptionReportedAt);
        Assert.True(await service.RefundAsync(order.Id));
        Assert.Equal(OrderStatus.Refunded, order.Status);
        Assert.Equal(7, product.Stock);
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

    [Fact]
    public async Task ReturnAfterSales_RestoresStockOnlyOnce_AndRefundDoesNotDoubleIt()
    {
        var (context, order, product) = await SeedAsync();
        order.Status = OrderStatus.Completed;
        await context.SaveChangesAsync();
        var service = new OrderService(context);

        var request = await service.CreateAfterSalesAsync(
            order.Id, order.UserId!, AfterSalesType.Return, "Item is damaged");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAfterSalesAsync(order.Id, order.UserId!, AfterSalesType.Exchange, "Second request"));
        Assert.True(await service.ReviewAfterSalesAsync(request.Id, true, "Approved"));
        Assert.True(await service.MarkAfterSalesReceivedAsync(request.Id));
        Assert.True(await service.CompleteAfterSalesAsync(request.Id));
        Assert.Equal(7, product.Stock);
        Assert.NotNull(order.InventoryRestoredAt);

        Assert.True(await service.RefundAsync(order.Id));
        Assert.Equal(OrderStatus.Refunded, order.Status);
        Assert.Equal(7, product.Stock);
    }

    [Fact]
    public async Task ExchangeAfterSales_CreatesReplacementOrderWithoutChangingStock()
    {
        var (context, order, product) = await SeedAsync();
        order.Status = OrderStatus.Completed;
        await context.SaveChangesAsync();
        var service = new OrderService(context);

        var request = await service.CreateAfterSalesAsync(
            order.Id, order.UserId!, AfterSalesType.Exchange, "Wrong color");
        Assert.True(await service.ReviewAfterSalesAsync(request.Id, true, null));
        Assert.True(await service.MarkAfterSalesReceivedAsync(request.Id));
        Assert.True(await service.CompleteAfterSalesAsync(request.Id));

        var replacement = await context.Orders.SingleAsync(item => item.ReplacementForOrderId == order.Id);
        Assert.Equal(OrderStatus.PendingShipment, replacement.Status);
        Assert.Equal(0, replacement.Total);
        Assert.Equal(5, product.Stock);
    }
}
