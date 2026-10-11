using OnlineStore.Models;

namespace OnlineStore.Services;

/// <summary>优惠券金额计算（纯函数，便于单元测试）。</summary>
public static class CouponCalculator
{
    /// <summary>
    /// 根据优惠券与订单金额计算抵扣金额。
    /// 百分比按订单金额折算，固定金额直接抵扣；四舍五入到 2 位，且不超过订单金额。
    /// </summary>
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
