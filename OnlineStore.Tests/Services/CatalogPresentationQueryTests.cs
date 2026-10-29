using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OnlineStore;
using OnlineStore.Areas.Admin.Controllers;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.ViewComponents;
using OnlineStore.ViewModels;

namespace OnlineStore.Tests.Services;

public class CatalogPresentationQueryTests
{
    [Theory]
    [InlineData(typeof(CategoryNavigationViewComponent))]
    [InlineData(typeof(CouponsController))]
    public void PresentationComponents_DoNotDependOnDatabaseContexts(Type componentType)
    {
        var dependencies = componentType.GetConstructors()
            .SelectMany(constructor => constructor.GetParameters());

        Assert.DoesNotContain(dependencies,
            parameter => typeof(DbContext).IsAssignableFrom(parameter.ParameterType));
    }

    [Fact]
    public async Task Navigation_PreservesCategoryOrderAndReturnsOnlyTwelveNewestActiveProducts()
    {
        await using var context = CreateContext();
        var category = new Category { Id = 1, Name = "A", NameEn = "Category A", DisplayOrder = 1 };
        var sameTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        category.Products = Enumerable.Range(1, 15).Select(id => new Product
        {
            Id = id, Name = $"Product {id}", NameEn = $"English {id}",
            IsActive = id != 15, CreatedAt = sameTime
        }).ToList();
        category.Products.Add(new Product { Id = 16, Name = "Newest", CreatedAt = sameTime.AddDays(1) });
        context.Categories.AddRange(category,
            new Category { Id = 2, Name = "B", DisplayOrder = 1 },
            new Category { Id = 3, Name = "Z", DisplayOrder = 0 });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await new CategoryService(context).GetNavigationAsync();

        Assert.Equal(new[] { 3, 1, 2 }, result.Select(item => item.Id));
        Assert.Empty(result[0].Products);
        Assert.Empty(result[2].Products);
        Assert.Equal("Category A", result[1].NameEn);
        Assert.Equal(new[] { 16, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4 },
            result[1].Products.Select(item => item.Id));
        Assert.Equal("English 14", result[1].Products[1].NameEn);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Navigation_RespectsCancellation()
    {
        await using var context = CreateContext();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new CategoryService(context).GetNavigationAsync(cancellation.Token));
    }

    [Fact]
    public async Task CouponOptions_PreserveSortAndIncludeInactiveProductsWithoutTracking()
    {
        await using var context = CreateContext();
        context.Categories.AddRange(
            new Category { Id = 1, Name = "B", DisplayOrder = 1 },
            new Category { Id = 2, Name = "A", DisplayOrder = 1 },
            new Category { Id = 3, Name = "Z", DisplayOrder = 0 });
        context.Products.AddRange(
            new Product { Id = 1, Name = "Beta", IsActive = true },
            new Product { Id = 2, Name = "Alpha", NameEn = "English Alpha", Sku = "ALPHA", IsActive = false });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await new CouponService(context).GetFormOptionsAsync();

        Assert.Equal(new[] { 3, 2, 1 }, result.Categories.Select(item => item.Id));
        Assert.Equal(new[] { 2, 1 }, result.Products.Select(item => item.Id));
        Assert.False(result.Products[0].IsActive);
        Assert.Equal("English Alpha", result.Products[0].NameEn);
        Assert.Equal("ALPHA", result.Products[0].Sku);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData("en", "Category", "Product · SKU-1")]
    [InlineData("zh-CN", "分类", "商品 · SKU-1")]
    public async Task CouponEdit_PreservesLocalisedLabelsAndSelectedScope(
        string culture, string expectedCategory, string expectedProduct)
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
        try
        {
            await using var context = CreateContext();
            var category = new Category { Id = 1, Name = "分类", NameEn = "Category" };
            var product = new Product { Id = 1, Name = "商品", NameEn = "Product", Sku = "SKU-1" };
            context.Coupons.Add(new Coupon
            {
                Id = 1, Code = "SCOPE", Scope = CouponScope.Products,
                Categories = new List<Category> { category }, Products = new List<Product> { product }
            });
            await context.SaveChangesAsync();
            using var services = new ServiceCollection().AddLogging().AddLocalization().BuildServiceProvider();
            var controller = new CouponsController(new CouponService(context),
                services.GetRequiredService<IStringLocalizer<SharedResource>>())
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

            var view = Assert.IsType<ViewResult>(await controller.Edit(1));
            var form = Assert.IsType<CouponFormViewModel>(view.Model);

            Assert.Equal(expectedCategory, Assert.Single(form.CategoryOptions).Text);
            Assert.Equal(expectedProduct, Assert.Single(form.ProductOptions).Text);
            Assert.Equal("1", form.ProductOptions[0].Value);
            Assert.Equal(new[] { 1 }, form.CategoryIds);
            Assert.Equal(new[] { 1 }, form.ProductIds);
            Assert.Equal("SCOPE", form.Code);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public async Task CouponCreate_ForwardsRequestCancellationToOptionsQuery()
    {
        await using var context = CreateContext();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var services = new ServiceCollection().AddLogging().AddLocalization().BuildServiceProvider();
        var controller = new CouponsController(new CouponService(context),
            services.GetRequiredService<IStringLocalizer<SharedResource>>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestAborted = cancellation.Token }
            }
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => controller.Create());
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
