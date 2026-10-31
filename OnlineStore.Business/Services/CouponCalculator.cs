using OnlineStore.Models;

namespace OnlineStore.Services;

public static class CouponCalculator
{
    public static bool TryCompute(Coupon coupon, decimal orderTotal, out decimal discount)
        => TryCompute(coupon, orderTotal, out discount, out _);

    public static bool TryCompute(Coupon coupon, decimal orderTotal, out decimal discount, out bool freeShipping)
    {
        discount = 0;
        freeShipping = false;
        if (coupon is null || orderTotal <= 0)
        {
            return false;
        }

        if (coupon.DiscountType == CouponDiscountType.FreeShipping)
        {
            freeShipping = true;
            return true;
        }

        if (coupon.DiscountValue <= 0)
        {
            return false;
        }

        var raw = coupon.DiscountType == CouponDiscountType.Percent
            ? orderTotal * coupon.DiscountValue / 100m
            : coupon.DiscountValue;

        if (coupon.MaxDiscountAmount.HasValue)
        {
            raw = Math.Min(raw, coupon.MaxDiscountAmount.Value);
        }

        discount = Math.Min(orderTotal, Math.Round(raw, 2, MidpointRounding.AwayFromZero));
        return discount > 0;
    }
}
