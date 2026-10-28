using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OnlineStore.Controllers;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.ViewModels;

namespace OnlineStore.Tests.Services;

public class CommerceRegressionTests
{
    private static async Task<ApplicationDbContext> CreateContextAsync(string? databaseName = null)
    {
        var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString()).Options);
        context.Shops.Add(new Shop { Id = 1, Name = "Shop" });
        context.Products.Add(new Product { Id = 1, Name = "Product", Price = 10m, Stock = 5, ShopId = 1 });
        await context.SaveChangesAsync();
        return context;
    }

    private static Order NewOrder(int quantity = 1, decimal price = 10m) => new()
    {
        OrderNo = Guid.NewGuid().ToString("N")[..30],
        UserId = "user",
        Items = new List<OrderItem> { new() { ProductId = 1, Price = price, Quantity = quantity } }
    };

    [Fact]
    public async Task AddCart_MaximumQuantity_DoesNotOverflow()
    {
        using var context = await CreateContextAsync();
        var cart = new CartService(new MemoryCartStore(), new ProductService(context), new ProductSkuService(context));
        await cart.AddAsync(1, 2);
        await cart.AddAsync(1, int.MaxValue);
        Assert.Equal(5, Assert.Single((await cart.GetCartAsync()).Items).Quantity);
    }

    [Fact]
    public async Task AddCart_InvalidSku_DoesNotCreateBaseProductRows()
    {
        using var context = await CreateContextAsync();
        var cart = new CartService(new MemoryCartStore(), new ProductService(context), new ProductSkuService(context));
        await cart.AddAsync(1, 5, 999);
        await cart.AddAsync(1, 5, 999);
        Assert.Empty((await cart.GetCartAsync()).Items);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(6, 10)]
    [InlineData(1, 9)]
    public async Task CreateOrder_InvalidQuantityStockOrPrice_DoesNotPersist(int quantity, decimal price)
    {
        using var context = await CreateContextAsync();
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => new OrderService(context).CreateAsync(NewOrder(quantity, price)));
        Assert.Empty(await context.Orders.ToListAsync());
        Assert.Equal(5, (await context.Products.FindAsync(1))!.Stock);
    }

    [Fact]
    public async Task CreateOrder_InactiveProduct_DoesNotPersist()
    {
        using var context = await CreateContextAsync();
        (await context.Products.FindAsync(1))!.IsActive = false;
        await context.SaveChangesAsync();
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => new OrderService(context).CreateAsync(NewOrder()));
        Assert.Empty(await context.Orders.ToListAsync());
    }

    [Fact]
    public async Task CreateOrder_RepeatedProductRows_ValidatesCombinedStock()
    {
        using var context = await CreateContextAsync();
        var order = NewOrder(3);
        order.Items.Add(new OrderItem { ProductId = 1, Price = 10m, Quantity = 3 });
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => new OrderService(context).CreateAsync(order));
        Assert.Equal(5, (await context.Products.FindAsync(1))!.Stock);
    }

    [Fact]
    public async Task CreateOrder_RecalculatesTotalAndConsumesCoupon()
    {
        using var context = await CreateContextAsync();
        var coupon = new Coupon { Code = "SAVE", CampaignName = "满减券", DiscountType = CouponDiscountType.Fixed, DiscountValue = 2,
            StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1), UsageLimit = 1 };
        context.Coupons.Add(coupon);
        await context.SaveChangesAsync();
        var order = NewOrder(2);
        order.CouponCode = "SAVE";
        order.Total = -100;
        await new OrderService(context).CreateAsync(order);
        Assert.Equal(18m, order.Total);
        Assert.Equal(2m, order.DiscountAmount);
        Assert.Equal("满减券", order.CouponName);
        Assert.Equal(1, coupon.UsedCount);
        var redemption = Assert.Single(await context.CouponRedemptions.ToListAsync());
        Assert.Equal(order.Id, redemption.OrderId);
        Assert.Equal("user", redemption.UserId);
        Assert.Equal(2m, redemption.DiscountAmount);
    }

    [Fact]
    public async Task CreateOrder_AddsShippingAndPersistsCheckoutDetails()
    {
        using var context = await CreateContextAsync();
        (await context.Products.FindAsync(1))!.ShippingFeeAmount = 6m;
        await context.SaveChangesAsync();
        var order = NewOrder(2);
        order.PaymentMethod = "card_demo";
        order.OrderNotes = "Leave at reception";

        await new OrderService(context).CreateAsync(order);

        Assert.Equal(6m, order.ShippingAmount);
        Assert.Equal(26m, order.Total);
        Assert.Equal("card_demo", order.PaymentMethod);
        Assert.Equal("Leave at reception", order.OrderNotes);
    }

    [Fact]
    public async Task CreateOrder_SnapshotsProductImage()
    {
        using var context = await CreateContextAsync();
        (await context.Products.FindAsync(1))!.ImageUrl = "/images/order-product.jpg";
        await context.SaveChangesAsync();
        var order = NewOrder();

        await new OrderService(context).CreateAsync(order);

        Assert.Equal("/images/order-product.jpg", Assert.Single(order.Items).ImageUrl);
    }

    [Fact]
    public async Task FreeShippingCoupon_RemovesCalculatedShipping()
    {
        using var context = await CreateContextAsync();
        (await context.Products.FindAsync(1))!.ShippingFeeAmount = 6m;
        context.Coupons.Add(new Coupon
        {
            Code = "FREESHIP",
            DiscountType = CouponDiscountType.FreeShipping,
            DiscountValue = 1,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1)
        });
        await context.SaveChangesAsync();
        var order = NewOrder(2);
        order.CouponCode = "FREESHIP";

        await new OrderService(context).CreateAsync(order);

        Assert.Equal(0m, order.ShippingAmount);
        Assert.Equal(20m, order.Total);
    }

    [Fact]
    public async Task CreateOrder_PerUserLimit_BlocksReuseUntilOrderIsCancelled()
    {
        using var context = await CreateContextAsync();
        var coupon = new Coupon
        {
            Code = "ONCE",
            DiscountType = CouponDiscountType.Fixed,
            DiscountValue = 2,
            PerUserLimit = 1,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1)
        };
        context.Coupons.Add(coupon);
        await context.SaveChangesAsync();

        var first = NewOrder();
        first.CouponCode = "ONCE";
        var service = new OrderService(context);
        await service.CreateAsync(first);

        var second = NewOrder();
        second.CouponCode = "ONCE";
        await Assert.ThrowsAsync<OrderValidationException>(() => new OrderService(context).CreateAsync(second));
        Assert.Equal(4, (await context.Products.FindAsync(1))!.Stock);

        Assert.True(await service.CancelAsync(first.Id));
        var redemption = Assert.Single(await context.CouponRedemptions.ToListAsync());
        Assert.NotNull(redemption.ReleasedAt);
        Assert.Equal(0, (await context.Coupons.FindAsync(coupon.Id))!.UsedCount);

        var third = NewOrder();
        third.CouponCode = "ONCE";
        await new OrderService(context).CreateAsync(third);
        Assert.Equal(1, (await context.Coupons.FindAsync(coupon.Id))!.UsedCount);
    }

    [Fact]
    public async Task Dashboard_DeletedOrderedProduct_DoesNotThrow()
    {
        using var context = await CreateContextAsync();
        var order = NewOrder();
        order.Total = 10;
        order.Status = OrderStatus.Delivered;
        context.Orders.Add(order);
        context.Orders.Add(new Order
        {
            OrderNo = Guid.NewGuid().ToString("N")[..30],
            UserId = "user",
            Total = 100,
            Status = OrderStatus.Delivered,
            ReplacementForOrderId = order.Id
        });
        await context.SaveChangesAsync();
        await new ProductService(context).DeleteAsync(1);
        var result = await new AdminDashboardService(context, new ProductService(context)).GetAsync();
        Assert.Equal(10m, result.TotalRevenue);
        Assert.Empty(result.TopProducts);
    }

    [Fact]
    public async Task Favorite_MissingProduct_DoesNotCreateReference()
    {
        using var context = await CreateContextAsync();
        Assert.False(await new FavoriteService(context).ToggleAsync("user", 999));
        Assert.Empty(await context.Favorites.ToListAsync());
    }

    [Fact]
    public async Task FavoriteCount_RemainsAfterAddingTheProductToCart()
    {
        using var context = await CreateContextAsync();
        var favorites = new FavoriteService(context);
        Assert.True(await favorites.ToggleAsync("user", 1));
        Assert.Equal(1, await favorites.CountAsync("user"));

        var cart = new CartService(new MemoryCartStore(), new ProductService(context), new ProductSkuService(context));
        await cart.AddAsync(1, 1);

        Assert.Equal(1, await favorites.CountAsync("user"));
        Assert.False(await favorites.ToggleAsync("user", 1));
        Assert.Equal(0, await favorites.CountAsync("user"));
    }

    [Fact]
    public async Task CreateOrder_SelectedSku_CancelRestoresBothStocksExactlyOnce()
    {
        using var context = await CreateContextAsync();
        var sku = new ProductSku { ProductId = 1, Color = "Blue", Spec = "Large", Price = 12m, Stock = 3 };
        context.ProductSkus.Add(sku);
        await context.SaveChangesAsync();
        var order = NewOrder(2, 12m);
        order.Items.Single().SkuId = sku.Id;
        var service = new OrderService(context);
        await service.CreateAsync(order);
        Assert.Equal(1, sku.Stock);
        Assert.Equal(3, (await context.Products.FindAsync(1))!.Stock);
        Assert.Equal(24m, order.Total);
        Assert.Equal("Blue", order.Items.Single().Color);
        Assert.True(await service.CancelAsync(order.Id));
        Assert.False(await service.CancelAsync(order.Id));
        Assert.Equal(3, sku.Stock);
        Assert.Equal(5, (await context.Products.FindAsync(1))!.Stock);
    }

    [Fact]
    public async Task CreateOrder_SkuStockInsufficient_DoesNotConsumeProductStock()
    {
        using var context = await CreateContextAsync();
        var sku = new ProductSku { ProductId = 1, Color = "Blue", Spec = "Large", Price = 10m, Stock = 1 };
        context.ProductSkus.Add(sku);
        await context.SaveChangesAsync();
        var order = NewOrder(2);
        order.Items.Single().SkuId = sku.Id;
        await Assert.ThrowsAsync<OrderValidationException>(() => new OrderService(context).CreateAsync(order));
        Assert.Equal(5, (await context.Products.FindAsync(1))!.Stock);
        Assert.Equal(1, (await context.ProductSkus.FindAsync(sku.Id))!.Stock);
        Assert.Empty(await context.Orders.ToListAsync());
    }

    [Fact]
    public async Task CreateOrder_TrackedPriceChangedInAnotherContext_RejectsStaleSnapshot()
    {
        var databaseName = Guid.NewGuid().ToString();
        using var context = await CreateContextAsync(databaseName);
        using var other = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName).Options);
        (await other.Products.FindAsync(1))!.Price = 20m;
        await other.SaveChangesAsync();
        var exception = await Assert.ThrowsAsync<OrderValidationException>(() => new OrderService(context).CreateAsync(NewOrder()));
        Assert.Equal("CheckoutPriceChanged", exception.ResourceKey);
        Assert.Empty(await context.Orders.ToListAsync());
    }

    [Fact]
    public async Task CreateOrder_CouponAlreadyConsumed_DoesNotConsumeStock()
    {
        using var context = await CreateContextAsync();
        context.Coupons.Add(new Coupon { Code = "SAVE", DiscountValue = 10, UsedCount = 1, UsageLimit = 1,
            StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1) });
        await context.SaveChangesAsync();
        var order = NewOrder();
        order.CouponCode = "SAVE";
        await Assert.ThrowsAsync<OrderValidationException>(() => new OrderService(context).CreateAsync(order));
        Assert.Equal(5, (await context.Products.FindAsync(1))!.Stock);
        Assert.Empty(await context.Orders.ToListAsync());
    }

    [Fact]
    public async Task CreateOrder_CouponDiscountReduced_DoesNotSilentlyIncreasePayableAmount()
    {
        using var context = await CreateContextAsync();
        context.Coupons.Add(new Coupon { Code = "SAVE", DiscountType = CouponDiscountType.Fixed, DiscountValue = 1,
            StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1) });
        await context.SaveChangesAsync();
        var order = NewOrder();
        order.CouponCode = "SAVE";
        order.DiscountAmount = 2m;
        await Assert.ThrowsAsync<OrderValidationException>(() => new OrderService(context).CreateAsync(order));
        Assert.Empty(await context.Orders.ToListAsync());
        Assert.Equal(5, (await context.Products.FindAsync(1))!.Stock);
    }

    [Fact]
    public async Task CancelOrder_TrackedStatusChangedInAnotherContext_DoesNotRestoreStock()
    {
        var databaseName = Guid.NewGuid().ToString();
        using var context = await CreateContextAsync(databaseName);
        var order = NewOrder();
        await new OrderService(context).CreateAsync(order);
        using var other = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName).Options);
        Assert.True(await new OrderService(other).ShipAsync(order.Id, "SF Express", "TEST00000001"));
        Assert.False(await new OrderService(context).CancelAsync(order.Id));
        Assert.Equal(4, (await context.Products.FindAsync(1))!.Stock);
    }

    [Fact]
    public async Task CreateProduct_AssignsExistingCategories()
    {
        using var context = await CreateContextAsync();
        context.Categories.Add(new Category { Id = 1, Name = "Category" });
        await context.SaveChangesAsync();
        var product = new Product { Name = "New product", Price = 5, Stock = 1, ShopId = 1 };
        var service = new ProductService(context);
        await service.SetCategoriesAsync(product, new[] { 1 });
        await service.CreateAsync(product);
        context.ChangeTracker.Clear();
        Assert.Single((await context.Products.Include(p => p.Categories).SingleAsync(p => p.Id == product.Id)).Categories);
    }

    [Fact]
    public async Task RefreshCart_UpdatesPriceAndRemovesUnavailableItems()
    {
        using var context = await CreateContextAsync();
        var cart = new CartService(new MemoryCartStore(), new ProductService(context), new ProductSkuService(context));
        Assert.True(await cart.AddAsync(1, 2));
        var product = (await context.Products.FindAsync(1))!;
        product.Price = 15m;
        product.Stock = 1;
        await context.SaveChangesAsync();
        Assert.Equal(20m, (await cart.GetCartAsync()).Total);
        Assert.Equal(15m, (await cart.RefreshAsync()).Total);
        product.IsActive = false;
        await context.SaveChangesAsync();
        Assert.Empty((await cart.RefreshAsync()).Items);
    }

    [Fact]
    public async Task Checkout_PriceChanged_RequiresResubmissionAndKeepsRefreshedCart()
    {
        using var context = await CreateContextAsync();
        context.Users.Add(new ApplicationUser { Id = "user", UserName = "shopper" });
        await context.SaveChangesAsync();
        var services = new ServiceCollection().AddLogging().AddSingleton(context);
        services.AddIdentityCore<ApplicationUser>().AddEntityFrameworkStores<ApplicationDbContext>();
        using var provider = services.BuildServiceProvider();
        var cart = new CartService(new MemoryCartStore(), new ProductService(context), new ProductSkuService(context));
        await cart.AddAsync(1, 2);
        (await context.Products.FindAsync(1))!.Price = 15m;
        await context.SaveChangesAsync();
        var controller = new OrdersController(new OrderService(context), cart, new CouponService(context),
            new DeliveryAddressService(context), provider.GetRequiredService<UserManager<ApplicationUser>>(),
            new TestLocalizer())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user") }, "test"))
                }
            }
        };
        var result = Assert.IsType<ViewResult>(await controller.Checkout(new CheckoutViewModel
        {
            UserName = "Shopper", Phone = "123", Address = "1 Street", City = "London", PostalCode = "AA1", Country = "UK"
        }));
        var model = Assert.IsType<CheckoutViewModel>(result.Model);
        Assert.False(controller.ModelState.IsValid);
        Assert.Equal(30m, model.Total);
        Assert.Equal(30m, (await cart.GetCartAsync()).Total);
        Assert.Empty(await context.Orders.ToListAsync());
        Assert.Equal(5, (await context.Products.FindAsync(1))!.Stock);
    }

    [Fact]
    public async Task AdminProductChildren_MissingParent_ReturnNotFound()
    {
        using var context = await CreateContextAsync();
        var products = new ProductService(context);
        var skuController = new OnlineStore.Areas.Admin.Controllers.ProductSkusController(
            new ProductSkuService(context), products, new TestLocalizer());
        var specController = new OnlineStore.Areas.Admin.Controllers.ProductSpecsController(
            new ProductSpecService(context), products, new TestLocalizer());
        Assert.IsType<NotFoundResult>(await skuController.Create(new ProductSku { ProductId = 999, Color = "Blue", Spec = "Large" }));
        Assert.IsType<NotFoundResult>(await specController.Create(new ProductSpec { ProductId = 999, Name = "Size", Value = "Large" }));
        Assert.Empty(await context.ProductSkus.ToListAsync());
        Assert.Empty(await context.ProductSpecs.ToListAsync());
    }

    [Fact]
    public async Task AdminProductChildren_EditCannotReassignParent()
    {
        using var context = await CreateContextAsync();
        context.Products.Add(new Product { Id = 2, Name = "Other product", ShopId = 1 });
        var sku = new ProductSku { ProductId = 1, Color = "Blue", Spec = "Large" };
        var spec = new ProductSpec { ProductId = 1, Name = "Size", Value = "Large" };
        context.AddRange(sku, spec);
        await context.SaveChangesAsync();
        var products = new ProductService(context);
        var skuController = new OnlineStore.Areas.Admin.Controllers.ProductSkusController(
            new ProductSkuService(context), products, new TestLocalizer());
        var specController = new OnlineStore.Areas.Admin.Controllers.ProductSpecsController(
            new ProductSpecService(context), products, new TestLocalizer());
        Assert.IsType<NotFoundResult>(await skuController.Edit(sku.Id, new ProductSku { Id = sku.Id, ProductId = 2 }));
        Assert.IsType<NotFoundResult>(await specController.Edit(spec.Id, new ProductSpec { Id = spec.Id, ProductId = 2 }));
        Assert.Equal(1, (await context.ProductSkus.FindAsync(sku.Id))!.ProductId);
        Assert.Equal(1, (await context.ProductSpecs.FindAsync(spec.Id))!.ProductId);
    }

    [Fact]
    public async Task CreateOrder_WrappedConcurrencyFailure_ReturnsRetryValidation()
    {
        var databaseName = Guid.NewGuid().ToString();
        using var seed = await CreateContextAsync(databaseName);
        using var context = new FailingSaveContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName).Options);
        var exception = await Assert.ThrowsAsync<OrderValidationException>(() => new OrderService(context).CreateAsync(NewOrder()));
        Assert.Equal("CheckoutRetry", exception.ResourceKey);
        Assert.Empty(await seed.Orders.ToListAsync());
        Assert.Equal(5, (await seed.Products.FindAsync(1))!.Stock);
    }

    private sealed class FailingSaveContext : ApplicationDbContext
    {
        public FailingSaveContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Execution strategy wrapper", new DbUpdateConcurrencyException("Conflict"));
    }

    private sealed class TestLocalizer : IStringLocalizer<SharedResource>
    {
        public LocalizedString this[string name] => new(name, name);
        public LocalizedString this[string name, params object[] arguments] => new(name, name);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
    }

    private sealed class MemoryCartStore : ICartStore
    {
        private List<CartItem> _items = new();
        public Task<List<CartItem>> GetAsync() => Task.FromResult(_items);
        public Task SaveAsync(List<CartItem> items)
        {
            _items = items;
            return Task.CompletedTask;
        }
    }
}
