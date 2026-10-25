using OnlineStore.Models;

namespace OnlineStore.Services;

public static class CouponCalculator
{

    public static bool TryCompute(Coupon coupon, decimal orderTotal, out decimal discount)
    {
        discount = 0;
        if (coupon is null || orderTotal <= 0 || coupon.DiscountValue <= 0)
        {
            return false;
        }

        var raw = coupon.DiscountType == CouponDiscountType.Percent
            ? orderTotal * coupon.DiscountValue / 100m
            : coupon.DiscountValue;

        discount = Math.Min(orderTotal, Math.Round(raw, 2, MidpointRounding.AwayFromZero));
        return discount > 0;
    }
}
