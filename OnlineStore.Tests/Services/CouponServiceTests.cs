using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Tests.Services;

public class CouponServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Coupon ValidPercent() => new()
    {
        Code = "SAVE10",
        DiscountType = CouponDiscountType.Percent,
        DiscountValue = 10,
        IsActive = true,
        StartDate = DateTime.UtcNow.AddDays(-1),
        EndDate = DateTime.UtcNow.AddDays(1)
    };

    [Fact]
    public async Task ValidateAsync_PercentDiscount_ReturnsDiscount()
    {
        using var context = CreateContext();
        context.Coupons.Add(ValidPercent());
        await context.SaveChangesAsync();
        var service = new CouponService(context);

        var result = await service.ValidateAsync("save10", 100m);

        Assert.True(result.Success);
        Assert.Equal(10m, result.DiscountAmount);
    }

    [Fact]
    public async Task ValidateAsync_UnknownCode_Fails()
    {
        using var context = CreateContext();
        var service = new CouponService(context);

        var result = await service.ValidateAsync("NOPE", 100m);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ValidateAsync_Inactive_Fails()
    {
        using var context = CreateContext();
        context.Coupons.Add(new Coupon
        {
            Code = "X", DiscountValue = 10, IsActive = false,
            StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1)
        });
        await context.SaveChangesAsync();
        var service = new CouponService(context);

        var result = await service.ValidateAsync("X", 100m);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ValidateAsync_Expired_Fails()
    {
        using var context = CreateContext();
        context.Coupons.Add(new Coupon
        {
            Code = "X", DiscountValue = 10, IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-2), EndDate = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();
        var service = new CouponService(context);

        var result = await service.ValidateAsync("X", 100m);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ValidateAsync_NotStarted_Fails()
    {
        using var context = CreateContext();
        context.Coupons.Add(new Coupon
        {
            Code = "X", DiscountValue = 10, IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(1), EndDate = DateTime.UtcNow.AddDays(2)
        });
        await context.SaveChangesAsync();
        var service = new CouponService(context);

        var result = await service.ValidateAsync("X", 100m);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ValidateAsync_UsageLimitReached_Fails()
    {
        using var context = CreateContext();
        context.Coupons.Add(new Coupon
        {
            Code = "X", DiscountValue = 10, IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1),
            UsageLimit = 1, UsedCount = 1
        });
        await context.SaveChangesAsync();
        var service = new CouponService(context);

        var result = await service.ValidateAsync("X", 100m);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ValidateAsync_MinOrderNotMet_Fails()
    {
        using var context = CreateContext();
        context.Coupons.Add(new Coupon
        {
            Code = "X", DiscountValue = 10, IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1),
            MinOrderAmount = 200
        });
        await context.SaveChangesAsync();
        var service = new CouponService(context);

        var result = await service.ValidateAsync("X", 100m);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ValidateAsync_PerUserLimit_BlocksRepeatedRedemption()
    {
        using var context = CreateContext();
        var coupon = ValidPercent();
        coupon.PerUserLimit = 1;
        context.Coupons.Add(coupon);
        await context.SaveChangesAsync();
        context.CouponRedemptions.Add(new CouponRedemption
        {
            CouponId = coupon.Id,
            UserId = "user-1",
            OrderId = 10,
            Code = coupon.Code
        });
        await context.SaveChangesAsync();

        var result = await new CouponService(context).ValidateAsync(coupon.Code, 100m, "user-1");

        Assert.False(result.Success);
        Assert.Equal("CouponPerUserLimit", result.Error);
    }

    [Fact]
    public async Task ValidateAsync_FirstOrderOnly_BlocksExistingCustomer()
    {
        using var context = CreateContext();
        var coupon = ValidPercent();
        coupon.FirstOrderOnly = true;
        context.Coupons.Add(coupon);
        context.Orders.Add(new Order { OrderNo = "OLD", UserId = "user-1", Total = 10m });
        await context.SaveChangesAsync();

        var result = await new CouponService(context).ValidateAsync(coupon.Code, 100m, "user-1");

        Assert.False(result.Success);
        Assert.Equal("CouponFirstOrderOnly", result.Error);
    }

    [Fact]
    public async Task ValidateAsync_CategoryScope_UsesOnlyEligibleItems()
    {
        using var context = CreateContext();
        var category = new Category { Id = 7, Name = "Eligible", Slug = "eligible" };
        var product = new Product
        {
            Id = 7,
            Name = "Eligible product",
            Slug = "eligible-product",
            Price = 80m,
            ShopId = 1,
            Categories = new List<Category> { category }
        };
        var coupon = ValidPercent();
        coupon.DiscountValue = 50;
        coupon.Scope = CouponScope.Categories;
        coupon.Categories.Add(category);
        context.Products.Add(product);
        context.Coupons.Add(coupon);
        await context.SaveChangesAsync();

        var result = await new CouponService(context).ValidateAsync(
            coupon.Code,
            80m,
            "user-1",
            new[] { new CouponEvaluationItem(7, 80m, 1) });

        Assert.True(result.Success);
        Assert.Equal(40m, result.DiscountAmount);
    }

    [Fact]
    public async Task ValidateAsync_ExcludeDiscountedItems_RejectsWhenAllItemsAreOnSale()
    {
        using var context = CreateContext();
        var coupon = ValidPercent();
        coupon.ExcludeDiscountedItems = true;
        context.Products.Add(new Product
        {
            Id = 8,
            Name = "Sale product",
            Slug = "sale-product",
            Price = 80m,
            OriginalPrice = 100m,
            ShopId = 1
        });
        context.Coupons.Add(coupon);
        await context.SaveChangesAsync();

        var result = await new CouponService(context).ValidateAsync(
            coupon.Code,
            80m,
            "user-1",
            new[] { new CouponEvaluationItem(8, 80m, 1) });

        Assert.False(result.Success);
        Assert.Equal("CouponItemsNotEligible", result.Error);
    }

    [Fact]
    public async Task ValidateAsync_MaximumDiscount_CapsPercentageCoupon()
    {
        using var context = CreateContext();
        var coupon = ValidPercent();
        coupon.DiscountValue = 50;
        coupon.MaxDiscountAmount = 15m;
        context.Coupons.Add(coupon);
        await context.SaveChangesAsync();

        var result = await new CouponService(context).ValidateAsync(coupon.Code, 100m);

        Assert.True(result.Success);
        Assert.Equal(15m, result.DiscountAmount);
    }

    [Fact]
    public async Task ValidateAsync_FreeShipping_ReturnsShippingBenefit()
    {
        using var context = CreateContext();
        var coupon = ValidPercent();
        coupon.DiscountType = CouponDiscountType.FreeShipping;
        coupon.DiscountValue = 0;
        context.Coupons.Add(coupon);
        await context.SaveChangesAsync();

        var result = await new CouponService(context).ValidateAsync(coupon.Code, 100m);

        Assert.True(result.Success);
        Assert.True(result.FreeShipping);
        Assert.Equal(0m, result.DiscountAmount);
    }

    [Fact]
    public async Task GetAvailableAsync_OrdersBestCouponFirst()
    {
        using var context = CreateContext();
        var smaller = ValidPercent();
        smaller.Code = "SMALL";
        smaller.DiscountType = CouponDiscountType.Fixed;
        smaller.DiscountValue = 5;
        var larger = ValidPercent();
        larger.Code = "LARGE";
        larger.DiscountType = CouponDiscountType.Fixed;
        larger.DiscountValue = 15;
        context.Coupons.AddRange(smaller, larger);
        await context.SaveChangesAsync();

        var results = await new CouponService(context).GetAvailableAsync("user-1", 100m, Array.Empty<CouponEvaluationItem>());

        Assert.Equal("LARGE", results[0].Coupon!.Code);
        Assert.Equal(15m, results[0].DiscountAmount);
    }

    [Fact]
    public async Task GenerateBatchAsync_CreatesUniqueCodesWithSameRules()
    {
        using var context = CreateContext();
        var source = ValidPercent();
        source.MaxDiscountAmount = 12m;
        source.PerUserLimit = 2;
        source.CampaignName = "Autumn";
        context.Coupons.Add(source);
        await context.SaveChangesAsync();

        var count = await new CouponService(context).GenerateBatchAsync(source.Id, 3, "AUTUMN");
        var generated = await context.Coupons
            .Where(coupon => coupon.Code.StartsWith("AUTUMN-"))
            .ToListAsync();

        Assert.Equal(3, count);
        Assert.Equal(3, generated.Count);
        Assert.Equal(3, generated.Select(coupon => coupon.Code).Distinct().Count());
        Assert.All(generated, coupon =>
        {
            Assert.Equal(12m, coupon.MaxDiscountAmount);
            Assert.Equal(2, coupon.PerUserLimit);
            Assert.StartsWith("Autumn #", coupon.CampaignName);
        });
        Assert.Equal(3, generated.Select(coupon => coupon.CampaignName).Distinct().Count());
    }

    [Fact]
    public async Task DeleteAsync_UsedCoupon_IsSoftDeletedForAuditHistory()
    {
        using var context = CreateContext();
        var coupon = ValidPercent();
        context.Coupons.Add(coupon);
        await context.SaveChangesAsync();
        context.CouponRedemptions.Add(new CouponRedemption
        {
            CouponId = coupon.Id,
            UserId = "user-1",
            OrderId = 99,
            Code = coupon.Code
        });
        await context.SaveChangesAsync();

        Assert.True(await new CouponService(context).DeleteAsync(coupon.Id));
        context.ChangeTracker.Clear();

        var stored = await context.Coupons.FindAsync(coupon.Id);
        Assert.NotNull(stored);
        Assert.True(stored.IsDeleted);
        Assert.False(stored.IsActive);
        Assert.Single(await context.CouponRedemptions.ToListAsync());
    }

    [Fact]
    public async Task GetCustomerCouponsAsync_ReportsAvailableScheduledUsedAndExpiredStates()
    {
        using var context = CreateContext();
        var now = DateTime.UtcNow;
        var available = ValidPercent();
        available.Code = "AVAILABLE";
        var scheduled = ValidPercent();
        scheduled.Code = "SCHEDULED";
        scheduled.StartDate = now.AddDays(1);
        scheduled.EndDate = now.AddDays(10);
        var used = ValidPercent();
        used.Code = "USED";
        used.PerUserLimit = 1;
        var expired = ValidPercent();
        expired.Code = "EXPIRED";
        expired.StartDate = now.AddDays(-10);
        expired.EndDate = now.AddDays(-2);
        context.Coupons.AddRange(available, scheduled, used, expired);
        context.Orders.Add(new Order { OrderNo = "OLD", UserId = "user-1", Total = 10m });
        await context.SaveChangesAsync();
        context.CouponRedemptions.Add(new CouponRedemption
        {
            CouponId = used.Id,
            UserId = "user-1",
            OrderId = 50,
            Code = used.Code
        });
        await context.SaveChangesAsync();

        var wallet = await new CouponService(context).GetCustomerCouponsAsync("user-1");
        var statuses = wallet.ToDictionary(item => item.Coupon.Code, item => item.Status);

        Assert.Equal(CouponWalletStatus.Available, statuses["AVAILABLE"]);
        Assert.Equal(CouponWalletStatus.Scheduled, statuses["SCHEDULED"]);
        Assert.Equal(CouponWalletStatus.Used, statuses["USED"]);
        Assert.Equal(CouponWalletStatus.Expired, statuses["EXPIRED"]);
    }

    [Fact]
    public async Task GenerateUniqueCodeAsync_UsesCampaignPrefixAndFallback()
    {
        using var context = CreateContext();
        var service = new CouponService(context);

        var campaignCode = await service.GenerateUniqueCodeAsync("Autumn Members");
        var fallbackCode = await service.GenerateUniqueCodeAsync("会员专享");

        Assert.StartsWith("AUTUMNMEMBERS-", campaignCode);
        Assert.Matches("^[A-Z0-9-]{6,50}$", campaignCode);
        Assert.StartsWith("ZHY-", fallbackCode);
        Assert.Matches("^[A-Z0-9-]{6,50}$", fallbackCode);
    }
}
