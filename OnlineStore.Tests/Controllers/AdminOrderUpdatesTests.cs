using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;
using AdminOrdersController = OnlineStore.Areas.Admin.Controllers.OrdersController;

namespace OnlineStore.Tests.Controllers;

public class AdminOrderUpdatesTests
{
    [Fact]
    public void AdminOrdersController_DoesNotExposeCancelAction()
    {
        var cancelAction = typeof(AdminOrdersController).GetMethod(
            "Cancel",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.Null(cancelAction);
    }

    [Theory]
    [InlineData(25, 10)]
    [InlineData(15, 10)]
    [InlineData(5, 5)]
    public async Task PollingVisibleOrders_AdvancesDeliveryEvenAfterAnotherOrderArrives(int firstId, int count)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        var database = Guid.NewGuid().ToString();
        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(database));
        builder.Services.AddScoped<IOrderService, OrderService>();
        builder.Services.AddLocalization();
        builder.Services.AddAuthorization();
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(AdminOrdersController).Assembly);
        await using var app = builder.Build();
        app.MapControllerRoute("admin", "{area:exists}/{controller}/{action=Index}").AllowAnonymous();
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            for (var id = 1; id <= 25; id++)
            {
                context.Orders.Add(new Order
                {
                    Id = id, OrderNo = $"LIVE-{id}", Status = OrderStatus.Shipped,
                    CreatedAt = new DateTime(2026, 1, 1).AddMinutes(id),
                    ShippedAt = DateTime.UtcNow.AddMinutes(-1)
                });
            }
            await context.SaveChangesAsync();
            context.Orders.Add(new Order { Id = 26, OrderNo = "NEW-ORDER", CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();
        }
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        var visibleIds = Enumerable.Range(firstId - count + 1, count).Reverse();
        var query = string.Join("&", visibleIds.Select(id => $"ids={id}"));
        var response = await client.GetAsync($"/Admin/Orders/OrderUpdates?{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var result = await response.Content.ReadFromJsonAsync<UpdateResponse>();

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(Enumerable.Range(firstId - count + 1, count).Reverse(), result.Orders.Select(order => order.Id));
        Assert.All(result.Orders, order =>
        {
            Assert.Equal(OrderStatus.Delivered, order.Status);
            Assert.False(string.IsNullOrWhiteSpace(order.StatusLabel));
        });
        using var verifyScope = app.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.All(await verifyContext.Orders.Where(order => order.Id <= 25).ToListAsync(), order =>
        {
            Assert.Equal(OrderStatus.Delivered, order.Status);
            Assert.NotNull(order.AutoConfirmAt);
        });
        Assert.Equal(OrderStatus.PendingShipment, (await verifyContext.Orders.FindAsync(26))!.Status);
        var oversized = await client.GetAsync("/Admin/Orders/OrderUpdates?" +
            string.Join("&", Enumerable.Range(1, 11).Select(id => $"ids={id}")));
        Assert.Equal(HttpStatusCode.BadRequest, oversized.StatusCode);
        var invalid = await client.GetAsync("/Admin/Orders/OrderUpdates?ids=-1");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    private sealed record UpdateResponse(bool Success, List<OrderUpdate> Orders);
    private sealed record OrderUpdate(int Id, string Status, string StatusLabel);
}
