using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.ViewModels;

namespace OnlineStore.Tests.Services;

/// <summary>
/// 商品业务层单元测试：验证公开目录的分页、分类/关键词/价格筛选与排序。
/// 使用 EF Core InMemory 提供程序，不依赖真实数据库（可测试性）。
/// </summary>
public class ProductServiceTests
{
    private static ApplicationDbContext CreateContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task SeedAsync(ApplicationDbContext context)
    {
        var c1 = new Category { Id = 1, Name = "电子产品", DisplayOrder = 1 };
        var c2 = new Category { Id = 2, Name = "图书", DisplayOrder = 2 };
        context.Categories.AddRange(c1, c2);
        var shop = new Shop { Id = 1, Name = "极客数码" };
        context.Shops.Add(shop);

        context.Products.AddRange(
            new Product
            {
                Id = 1, Name = "无线耳机", Price = 299m, Stock = 10, IsActive = true, ShopId = 1,
                Categories = new List<Category> { c1 },
                Reviews = new List<Review>
                {
                    new() { Rating = 5, UserName = "测试用户一" },
                    new() { Rating = 4, UserName = "测试用户二" }
                }
            },
            new Product
            {
                Id = 2, Name = "机械键盘", Price = 459m, Stock = 5, IsActive = true, ShopId = 1,
                Categories = new List<Category> { c1 },
                Reviews = new List<Review> { new() { Rating = 3, UserName = "测试用户三" } }
            },
            new Product { Id = 3, Name = "百年孤独", Price = 55m, Stock = 0, IsActive = true, ShopId = 1, Categories = new List<Category> { c2 } },
            new Product { Id = 4, Name = "已下架商品", Price = 999m, Stock = 5, IsActive = false, ShopId = 1, Categories = new List<Category> { c1 } });

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsOnlyActiveProducts()
    {
        using var context = CreateContext();
        await SeedAsync(context);
        var service = new ProductService(context);

        var result = await service.GetPagedAsync(new ProductQuery());

        Assert.Equal(3, result.TotalItems);
        Assert.DoesNotContain(result.Items, p => p.Id == 4);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByCategory()
    {
        using var context = CreateContext();
        await SeedAsync(context);
        var service = new ProductService(context);

        var result = await service.GetPagedAsync(new ProductQuery { CategoryId = 1 });

        Assert.Equal(2, result.TotalItems);
        Assert.All(result.Items, p => Assert.Contains(p.Categories, c => c.Id == 1));
    }

    [Fact]
    public async Task GetPagedAsync_FiltersBySearchTerm()
    {
        using var context = CreateContext();
        await SeedAsync(context);
        var service = new ProductService(context);

        var result = await service.GetPagedAsync(new ProductQuery { SearchTerm = "百年" });

        Assert.Single(result.Items);
        Assert.Equal(3, result.Items[0].Id);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByPriceRange()
    {
        using var context = CreateContext();
        await SeedAsync(context);
        var service = new ProductService(context);

        var result = await service.GetPagedAsync(new ProductQuery { MinPrice = 100m, MaxPrice = 400m });

        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].Id);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByBrand()
    {
        using var context = CreateContext();
        await SeedAsync(context);
        var service = new ProductService(context);

        var result = await service.GetPagedAsync(new ProductQuery { ShopId = 1 });

        Assert.Equal(3, result.TotalItems);
        Assert.Contains(result.Brands, brand => brand.Id == 1);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByAverageRating()
    {
        using var context = CreateContext();
        await SeedAsync(context);
        var service = new ProductService(context);

        var result = await service.GetPagedAsync(new ProductQuery { MinRating = 4.5 });

        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].Id);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersInStockProducts()
    {
        using var context = CreateContext();
        await SeedAsync(context);
        var service = new ProductService(context);

        var result = await service.GetPagedAsync(new ProductQuery { InStockOnly = true });

        Assert.Equal(2, result.TotalItems);
        Assert.All(result.Items, product => Assert.True(product.Stock > 0));
    }

    [Fact]
    public async Task GetPagedAsync_PaginatesCorrectly()
    {
        using var context = CreateContext();
        await SeedAsync(context);
        var service = new ProductService(context);

        var result = await service.GetPagedAsync(new ProductQuery { PageSize = 2, Page = 2 });

        Assert.Equal(3, result.TotalItems);
        Assert.Equal(2, result.TotalPages);
        Assert.Single(result.Items); // 第 2 页只剩 1 件
    }

    [Fact]
    public async Task GetPagedAsync_SortsByPriceAscending()
    {
        using var context = CreateContext();
        await SeedAsync(context);
        var service = new ProductService(context);

        var result = await service.GetPagedAsync(new ProductQuery { SortBy = "price_asc" });

        Assert.Equal(new[] { 55m, 299m, 459m }, result.Items.Select(p => p.Price).ToArray());
    }

    [Fact]
    public async Task GetSaleItemsAsync_ReturnsOnlyOnSaleProducts()
    {
        using var context = CreateContext();
        await SeedAsync(context);
        var onSale = await context.Products.FindAsync(1);
        onSale!.OriginalPrice = 399m;
        await context.SaveChangesAsync();

        var service = new ProductService(context);
        var result = await service.GetSaleItemsAsync(8);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.True(result[0].IsOnSale);
    }

    [Fact]
    public async Task SetCategoriesAsync_PreservesExistingCategory_WhenProductIsEdited()
    {
        var databaseName = Guid.NewGuid().ToString();
        using (var seedContext = CreateContext(databaseName))
        {
            await SeedAsync(seedContext);
        }

        using var context = CreateContext(databaseName);
        var service = new ProductService(context);
        var product = await service.GetByIdAsync(1);
        Assert.NotNull(product);

        await service.SetCategoriesAsync(product!, new[] { 1 });
        await service.UpdateAsync(product!);

        context.ChangeTracker.Clear();
        var savedProduct = await context.Products
            .Include(p => p.Categories)
            .SingleAsync(p => p.Id == 1);

        Assert.Single(savedProduct.Categories);
        Assert.Equal(1, savedProduct.Categories.Single().Id);
    }

    [Fact]
    public async Task SetCategoriesAsync_ReplacesExistingCategories_WhenProductIsEdited()
    {
        var databaseName = Guid.NewGuid().ToString();
        using (var seedContext = CreateContext(databaseName))
        {
            await SeedAsync(seedContext);
        }

        using var context = CreateContext(databaseName);
        var service = new ProductService(context);
        var product = await service.GetByIdAsync(1);
        Assert.NotNull(product);

        await service.SetCategoriesAsync(product!, new[] { 2 });
        await service.UpdateAsync(product!);

        context.ChangeTracker.Clear();
        var savedProduct = await context.Products
            .Include(p => p.Categories)
            .SingleAsync(p => p.Id == 1);

        Assert.Single(savedProduct.Categories);
        Assert.Equal(2, savedProduct.Categories.Single().Id);
    }
}
