using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.ViewModels;
using AdminOrdersController = OnlineStore.Areas.Admin.Controllers.OrdersController;

namespace OnlineStore.Tests.Controllers;

public class AdminOrderPaginationTests
{
    [Theory]
    [InlineData("orders", 2, 2, 15, 10)]
    [InlineData("after-sales", 2, 2, 15, 10)]
    [InlineData("orders", 3, 3, 5, 5)]
    [InlineData("after-sales", 3, 3, 5, 5)]
    [InlineData("orders", 0, 1, 25, 10)]
    [InlineData("after-sales", 99, 3, 5, 5)]
    public async Task PaginationQuery_ReturnsRequestedSectionAndRecords(
        string section, int page, int expectedPage, int firstId, int count)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        var database = Guid.NewGuid().ToString();
        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(database));
        builder.Services.AddScoped<IOrderService, OrderService>();
        builder.Services.AddLocalization();
        builder.Services.AddAuthorization();
        builder.Services.AddControllersWithViews(options => options.Filters.Add<ModelResultFilter>())
            .AddApplicationPart(typeof(AdminOrdersController).Assembly);
        await using var app = builder.Build();
        app.UseDeveloperExceptionPage();
        app.MapControllerRoute("admin", "{area:exists}/{controller}/{action=Index}").AllowAnonymous();
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            for (var id = 1; id <= 25; id++)
            {
                var date = new DateTime(2026, 1, 1).AddMinutes(id);
                context.Orders.Add(new Order { Id = id, OrderNo = $"PAGE-{id}", CreatedAt = date });
                context.AfterSalesRequests.Add(new AfterSalesRequest { Id = id, OrderId = id, RequestedAt = date });
            }
            await context.SaveChangesAsync();
        }
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        var response = await client.GetAsync($"/Admin/Orders?section={section}&page={page}");
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var result = await response.Content.ReadFromJsonAsync<PageResponse>();

        Assert.NotNull(result);
        Assert.Equal(section, result.Section);
        Assert.Equal(expectedPage, result.Page);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(Enumerable.Range(firstId - count + 1, count).Reverse(), result.Ids);
    }

    public sealed record PageResponse(string Section, int Page, int TotalPages, int[] Ids);

    private sealed class ModelResultFilter : IResultFilter
    {
        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is not ViewResult { Model: AdminOrdersViewModel model }) return;
            var afterSales = model.ActiveSection == "after-sales";
            context.Result = new JsonResult(new PageResponse(model.ActiveSection,
                afterSales ? model.AfterSalesPage : model.OrdersPage,
                afterSales ? model.AfterSalesTotalPages : model.OrdersTotalPages,
                afterSales ? model.AfterSales.Select(item => item.Id).ToArray()
                    : model.Orders.Select(item => item.Id).ToArray()));
        }

        public void OnResultExecuted(ResultExecutedContext context) { }
    }
}
