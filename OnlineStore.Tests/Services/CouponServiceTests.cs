using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Tests.Services;

/// <summary>优惠券服务单元测试：校验有效期/状态/上限/最低金额/大小写与计数。</summary>
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
    public async Task IncrementUsageAsync_BumpsUsedCount()
    {
        using var context = CreateContext();
        var coupon = ValidPercent();
        context.Coupons.Add(coupon);
        await context.SaveChangesAsync();
        var service = new CouponService(context);

        await service.IncrementUsageAsync(coupon.Id);

        Assert.Equal(1, coupon.UsedCount);
    }
}
