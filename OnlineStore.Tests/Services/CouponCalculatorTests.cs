using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Tests.Services;

public class CouponCalculatorTests
{
    private static Coupon Percent(decimal value) => new()
    {
        DiscountType = CouponDiscountType.Percent,
        DiscountValue = value
    };

    private static Coupon Fixed(decimal value) => new()
    {
        DiscountType = CouponDiscountType.Fixed,
        DiscountValue = value
    };

    [Fact]
    public void TryCompute_Percent_ReturnsDiscount()
    {
        Assert.True(CouponCalculator.TryCompute(Percent(10), 100m, out var discount));
        Assert.Equal(10m, discount);
    }

    [Fact]
    public void TryCompute_Fixed_ReturnsDiscount()
    {
        Assert.True(CouponCalculator.TryCompute(Fixed(15), 100m, out var discount));
        Assert.Equal(15m, discount);
    }

    [Fact]
    public void TryCompute_RoundsToTwoDecimals()
    {
        Assert.True(CouponCalculator.TryCompute(Percent(33), 99.99m, out var discount));
        Assert.Equal(33.00m, discount);
    }

    [Fact]
    public void TryCompute_CapsAtOrderTotal()
    {
        Assert.True(CouponCalculator.TryCompute(Fixed(200), 100m, out var discount));
        Assert.Equal(100m, discount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void TryCompute_NonPositiveValue_ReturnsFalse(decimal value)
    {
        Assert.False(CouponCalculator.TryCompute(Percent(value), 100m, out _));
    }

    [Fact]
    public void TryCompute_ZeroOrderTotal_ReturnsFalse()
    {
        Assert.False(CouponCalculator.TryCompute(Percent(10), 0m, out _));
    }
}
