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
    public async Task RefundAsync_Shipped_RequiresTransitInterceptionResolution()
    {
        var (context, order, product) = await SeedAsync();
        order.Status = OrderStatus.Shipped;
        await context.SaveChangesAsync();
        var service = new OrderService(context);

        var ok = await service.RefundAsync(order.Id);

        Assert.False(ok);
        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.Equal(5, product.Stock);
    }

    [Fact]
    public async Task TransitCancellation_PausesDelivery_AndInterceptionRefundsOnce()
    {
        var (context, order, product) = await SeedAsync();
        order.Status = OrderStatus.Shipped;
        order.ShippedAt = DateTime.UtcNow.AddSeconds(-20);
        await context.SaveChangesAsync();
        var service = new OrderService(context);

        var request = await service.CreateAfterSalesAsync(
            order.Id,
            order.UserId!,
            AfterSalesType.TransitCancellation,
            "No longer needed");
        await service.GetTrackingAsync(order.Id);
        await service.ProcessPendingOrderStatesAsync();

        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.Equal(AfterSalesStatus.Pending, request.Status);
        Assert.False(await service.ReviewAfterSalesAsync(request.Id, true, null));

        Assert.True(await service.ResolveTransitCancellationAsync(request.Id, true, "Parcel intercepted"));
        Assert.Equal(OrderStatus.Refunded, order.Status);
        Assert.Equal(AfterSalesStatus.Completed, request.Status);
        Assert.Equal(7, product.Stock);
        Assert.NotNull(order.InventoryRestoredAt);
        Assert.False(await service.ResolveTransitCancellationAsync(request.Id, true, null));
        Assert.Equal(7, product.Stock);
    }

    [Fact]
    public async Task TransitCancellation_FailureContinuesDelivery_AndAllowsReturnAfterDelivery()
    {
        var (context, order, product) = await SeedAsync();
        order.Status = OrderStatus.Shipped;
        order.ShippedAt = DateTime.UtcNow.AddSeconds(-20);
        await context.SaveChangesAsync();
        var service = new OrderService(context);

        var transit = await service.CreateAfterSalesAsync(
            order.Id,
            order.UserId!,
            AfterSalesType.TransitCancellation,
            "Changed my mind");
        Assert.True(await service.ResolveTransitCancellationAsync(transit.Id, false, "Courier could not intercept"));

        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.Equal(5, product.Stock);
        await service.ProcessPendingOrderStatesAsync();
        Assert.Equal(OrderStatus.Delivered, order.Status);

        var returnRequest = await service.CreateAfterSalesAsync(
            order.Id,
            order.UserId!,
            AfterSalesType.Return,
            "Refuse delivery and return");
        Assert.Equal(AfterSalesType.Return, returnRequest.Type);
        Assert.Equal(AfterSalesStatus.Pending, returnRequest.Status);
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
    public async Task GetCustomerOrderUpdates_ReturnsOnlyOwnedOrders_TracksAfterSalesState()
    {
        var (context, order, _) = await SeedAsync();
        order.Status = OrderStatus.Shipped;
        context.Orders.Add(new Order
        {
            OrderNo = "OS-OTHER",
            UserId = "user2",
            Total = 10m,
            Status = OrderStatus.Delivered
        });
        await context.SaveChangesAsync();
        var service = new OrderService(context);
        var request = await service.CreateAfterSalesAsync(
            order.Id,
            order.UserId!,
            AfterSalesType.TransitCancellation,
            "No longer needed");

        var updates = await service.GetCustomerOrderUpdatesAsync(
            order.UserId!,
            new[] { order.Id, 2 });

        var update = Assert.Single(updates);
        Assert.Equal(order.Id, update.Id);
        Assert.Equal(OrderStatus.Shipped, update.Status);
        Assert.Contains($"{request.Id}:{AfterSalesStatus.Pending}", update.StateKey);
    }

    [Fact]
    public async Task ActiveItemCount_CountsQuantitiesFromOrderCreationUntilAfterSalesIsClosed()
    {
        var (context, order, _) = await SeedAsync();
        var service = new OrderService(context);

        Assert.Equal(2, await service.GetActiveItemCountAsync(order.UserId!));
        order.Status = OrderStatus.Delivered;
        await context.SaveChangesAsync();
        var request = await service.CreateAfterSalesAsync(
            order.Id,
            order.UserId!,
            AfterSalesType.Return,
            "Return requested");

        order.Status = OrderStatus.Completed;
        await context.SaveChangesAsync();
        Assert.Equal(2, await service.GetActiveItemCountAsync(order.UserId!));

        Assert.True(await service.ReviewAfterSalesAsync(request.Id, false, "Closed"));
        Assert.Equal(0, await service.GetActiveItemCountAsync(order.UserId!));
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
        Assert.Equal(1, await service.CountOpenAfterSalesAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAfterSalesAsync(order.Id, order.UserId!, AfterSalesType.Exchange, "Second request"));
        Assert.True(await service.ReviewAfterSalesAsync(request.Id, true, "Approved"));
        Assert.Equal(1, await service.CountOpenAfterSalesAsync());
        Assert.False(await service.MarkAfterSalesReceivedAsync(request.Id));
        Assert.True(await service.SubmitReturnShipmentAsync(
            request.Id, order.UserId!, "顺丰速运", "RETURN000001"));
        Assert.Equal(1, await service.CountOpenAfterSalesAsync());
        Assert.True(await service.MarkAfterSalesReceivedAsync(request.Id));
        Assert.Equal(1, await service.CountOpenAfterSalesAsync());
        Assert.True(await service.CompleteAfterSalesAsync(request.Id));
        Assert.Equal(0, await service.CountOpenAfterSalesAsync());
        Assert.Equal(7, product.Stock);
        Assert.NotNull(order.InventoryRestoredAt);
        Assert.Equal(OrderStatus.Refunded, order.Status);

        Assert.False(await service.RefundAsync(order.Id));
        Assert.Equal(7, product.Stock);
    }

    [Fact]
    public async Task ExchangeAfterSales_CreatesReplacementOrderWithoutChangingStock()
    {
        var (context, order, product) = await SeedAsync();
        order.Status = OrderStatus.Delivered;
        await context.SaveChangesAsync();
        var service = new OrderService(context);

        var request = await service.CreateAfterSalesAsync(
            order.Id, order.UserId!, AfterSalesType.Exchange, "Wrong color");
        Assert.True(await service.ReviewAfterSalesAsync(request.Id, true, null));
        Assert.True(await service.SubmitReturnShipmentAsync(
            request.Id, order.UserId!, "顺丰速运", "RETURN000002"));
        Assert.True(await service.MarkAfterSalesReceivedAsync(request.Id));
        Assert.True(await service.CompleteAfterSalesAsync(request.Id));

        var replacement = await context.Orders.SingleAsync(item => item.ReplacementForOrderId == order.Id);
        Assert.Equal(OrderStatus.PendingShipment, replacement.Status);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(2, await service.GetActiveItemCountAsync(order.UserId!));
        Assert.Equal(0, replacement.Total);
        Assert.Equal(5, product.Stock);
    }

    [Fact]
    public async Task ReturnTracking_RequiresApprovedOwnerAndUsesReverseRouteUntilWarehouseReceipt()
    {
        var (context, order, _) = await SeedAsync();
        order.Status = OrderStatus.Completed;
        await context.SaveChangesAsync();
        var service = new OrderService(context);
        var request = await service.CreateAfterSalesAsync(
            order.Id,
            order.UserId!,
            AfterSalesType.Return,
            "Return needed");
        Assert.True(await service.ReviewAfterSalesAsync(request.Id, true, null));

        Assert.Null(await service.GetReturnTrackingAsync(request.Id, order.UserId));
        Assert.False(await service.SubmitReturnShipmentAsync(
            request.Id, "another-user", "顺丰速运", "RETURN000003"));
        Assert.True(await service.SubmitReturnShipmentAsync(
            request.Id, order.UserId!, "顺丰速运", "RETURN000003"));
        Assert.Equal(AfterSalesStatus.Returning, request.Status);

        Assert.Null(await service.GetReturnTrackingAsync(request.Id, "another-user"));
        var tracking = await service.GetReturnTrackingAsync(request.Id, order.UserId);
        Assert.NotNull(tracking);
        Assert.Equal("RETURN000003", tracking.TrackingNumber);
        Assert.Equal(4, tracking.Nodes.Count);
        Assert.False(tracking.Nodes[^1].IsReached);

        Assert.True(await service.MarkAfterSalesReceivedAsync(request.Id));
        tracking = await service.GetReturnTrackingAsync(request.Id, order.UserId);
        Assert.NotNull(tracking);
        Assert.Equal(AfterSalesStatus.Received, tracking.Status);
        Assert.True(tracking.Nodes[^1].IsReached);
    }

    [Fact]
    public async Task RefundAsync_WithActiveAfterSales_RequiresRequestResolution()
    {
        var (context, order, product) = await SeedAsync();
        order.Status = OrderStatus.Completed;
        await context.SaveChangesAsync();
        var service = new OrderService(context);
        await service.CreateAfterSalesAsync(
            order.Id,
            order.UserId!,
            AfterSalesType.Return,
            "Return needed");

        Assert.False(await service.RefundAsync(order.Id));
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(5, product.Stock);
    }
}
