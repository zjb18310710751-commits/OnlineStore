using OnlineStore.Models;

namespace OnlineStore.Services;

/// <summary>优惠券校验/计算结果。</summary>
public record CouponApplyResult(bool Success, string? Error, decimal DiscountAmount, Coupon? Coupon);

/// <summary>优惠券业务抽象。</summary>
public interface ICouponService
{
    Task<IReadOnlyList<Coupon>> GetAllAsync();

    Task<Coupon?> GetByIdAsync(int id);

    Task<Coupon> CreateAsync(Coupon coupon);

    Task UpdateAsync(Coupon coupon);

    Task<bool> DeleteAsync(int id);

    /// <summary>校验优惠码是否可用，并计算抵扣金额。</summary>
    Task<CouponApplyResult> ValidateAsync(string code, decimal orderTotal);

    Task IncrementUsageAsync(int id);
}
