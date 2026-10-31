using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OnlineStore.Controllers;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.ViewModels;

namespace OnlineStore.Tests.Controllers;

public class PublicRequestSafetyTests
{
    [Theory]
    [InlineData("")]
    [InlineData("unexpected")]
    [InlineData("HIGHEST")]
    public async Task ReviewSortQuery_IsNormalizedWithoutServerError(string sort)
    {
        await using var app = CreateApplication(false);
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Products.Add(new Product { Id = 1, Name = "Review test", Slug = "review-test", IsActive = true,
                Shop = new Shop { Name = "Review shop" } });
            await context.SaveChangesAsync();
        }
        await app.StartAsync();
        using var client = CreateClient(app);
        var response = await client.GetAsync($"/Products/Reviews/1?sort={sort}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SortResponse>();
        Assert.Equal(sort == "HIGHEST" ? "highest" : "newest", result!.Sort);
    }

    [Fact]
    public async Task ProductionErrorPage_DoesNotQueryUnavailableDatabaseOrExposeException()
    {
        await using var app = CreateApplication(true);
        await app.StartAsync();
        using var client = CreateClient(app);
        var response = await client.GetAsync("/simulate-failure");
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var categories = (UnavailableCategoryProxy)app.Services.GetRequiredService<ICategoryService>();
        Assert.Equal(0, categories.CallCount);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<html", body);
        Assert.Contains("href=\"/\"", body);
        Assert.DoesNotContain("PRIVATE-DATABASE-ERROR", body);
        Assert.DoesNotContain("InvalidOperationException", body);
    }

    private static WebApplication CreateApplication(bool unavailableDatabase)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(HomeController).Assembly.FullName,
            EnvironmentName = "Production",
            ContentRootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../OnlineStore"))
        });
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        var database = Guid.NewGuid().ToString();
        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(database));
        builder.Services.AddIdentityCore<ApplicationUser>().AddEntityFrameworkStores<ApplicationDbContext>();
        builder.Services.AddScoped<IProductService, ProductService>();
        builder.Services.AddScoped<IReviewService, ReviewService>();
        builder.Services.AddScoped<IFavoriteService, FavoriteService>();
        builder.Services.AddScoped<IOrderService, OrderService>();
        builder.Services.AddScoped<ICouponService, CouponService>();
        if (unavailableDatabase)
            builder.Services.AddSingleton(DispatchProxy.Create<ICategoryService, UnavailableCategoryProxy>());
        else
            builder.Services.AddScoped<ICategoryService, CategoryService>();
        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
        builder.Services.AddAuthorization();
        builder.Services.AddControllersWithViews(options => options.Filters.Add<ReviewModelFilter>())
            .AddApplicationPart(typeof(HomeController).Assembly);
        var app = builder.Build();
        app.UseExceptionHandler("/Home/Error");
        app.MapGet("/simulate-failure", () => Task.FromException(new InvalidOperationException("PRIVATE-DATABASE-ERROR")));
        app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
        return app;
    }

    private static HttpClient CreateClient(WebApplication app) => new()
    {
        BaseAddress = new Uri(app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single())
    };

    public class UnavailableCategoryProxy : DispatchProxy
    {
        public int CallCount { get; private set; }

        protected override object? Invoke(MethodInfo? method, object?[]? arguments)
        {
            CallCount++;
            throw new InvalidOperationException("PRIVATE-DATABASE-ERROR");
        }
    }

    private sealed class ReviewModelFilter : IResultFilter
    {
        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is ViewResult { Model: ProductReviewsPageViewModel model })
                context.Result = new JsonResult(new SortResponse(model.Sort));
        }

        public void OnResultExecuted(ResultExecutedContext context) { }
    }

    private sealed record SortResponse(string Sort);
}
