using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Tests.Services;

public class OrderReceiptSafetyTests
{
    [Theory]
    [InlineData(AfterSalesStatus.Pending)]
    [InlineData(AfterSalesStatus.Approved)]
    [InlineData(AfterSalesStatus.Returning)]
    [InlineData(AfterSalesStatus.Received)]
    public async Task ConfirmReceipt_ActiveAfterSalesPreservesDeliveryAndInventory(string requestStatus)
    {
        var options = Options(Guid.NewGuid().ToString());
        await using var context = new ApplicationDbContext(options);
        await SeedAsync(context);
        context.AfterSalesRequests.Add(new AfterSalesRequest
        {
            OrderId = 1,
            UserId = "buyer",
            Type = AfterSalesType.Return,
            Status = requestStatus,
            Reason = "Return requested"
        });
        await context.SaveChangesAsync();

        Assert.False(await new OrderService(context).ConfirmReceiptAsync(1, "buyer"));

        await using var verify = new ApplicationDbContext(options);
        var order = await verify.Orders.SingleAsync();
        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.Null(order.SignedAt);
        Assert.Null(order.InventoryRestoredAt);
        Assert.Equal(5, (await verify.Products.SingleAsync()).Stock);
        Assert.Equal(requestStatus, (await verify.AfterSalesRequests.SingleAsync()).Status);
    }

    [Fact]
    public async Task ConfirmReceipt_ConcurrentStatusChangeReturnsFalseWithoutOverwritingIt()
    {
        var database = Guid.NewGuid().ToString();
        var options = Options(database);
        await using (var seed = new ApplicationDbContext(options))
        {
            await SeedAsync(seed);
        }

        var competingUpdate = new BeforeSaveInterceptor(async () =>
        {
            await using var other = new ApplicationDbContext(options);
            Assert.True(await new OrderService(other).ReportReceiptExceptionAsync(1, "Missing parcel", "admin"));
        });
        var requestOptions = new DbContextOptionsBuilder<ApplicationDbContext>(options)
            .AddInterceptors(competingUpdate).Options;
        await using var request = new ApplicationDbContext(requestOptions);

        Assert.False(await new OrderService(request).ConfirmReceiptAsync(1, "buyer"));

        await using var verify = new ApplicationDbContext(options);
        var order = await verify.Orders.SingleAsync();
        Assert.Equal(OrderStatus.ReceiptException, order.Status);
        Assert.Equal("Missing parcel", order.ReceiptExceptionReason);
        Assert.Null(order.SignedAt);
        Assert.Equal(5, (await verify.Products.SingleAsync()).Stock);
    }

    [Fact]
    public async Task ConfirmReceipt_ReloadsPreviouslyReadOrderBeforeDeciding()
    {
        var options = Options(Guid.NewGuid().ToString());
        await using var request = new ApplicationDbContext(options);
        await SeedAsync(request);
        await using (var other = new ApplicationDbContext(options))
        {
            Assert.True(await new OrderService(other).ReportReceiptExceptionAsync(1, "Missing parcel", "admin"));
        }

        Assert.False(await new OrderService(request).ConfirmReceiptAsync(1, "buyer"));
    }

    private static DbContextOptions<ApplicationDbContext> Options(string database) =>
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(database).Options;

    private static async Task SeedAsync(ApplicationDbContext context)
    {
        context.Products.Add(new Product { Id = 1, Name = "Product", Price = 10m, Stock = 5, ShopId = 1 });
        context.Orders.Add(new Order
        {
            Id = 1,
            OrderNo = "RECEIPT-TEST",
            UserId = "buyer",
            Status = OrderStatus.Delivered,
            Items = new List<OrderItem> { new() { ProductId = 1, ProductName = "Product", Price = 10m, Quantity = 1 } }
        });
        await context.SaveChangesAsync();
    }

    private sealed class BeforeSaveInterceptor(Func<Task> beforeSave) : SaveChangesInterceptor
    {
        private bool _invoked;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (!_invoked)
            {
                _invoked = true;
                await beforeSave();
            }
            return result;
        }
    }
}
