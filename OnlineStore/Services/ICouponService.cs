using OnlineStore.Models;

namespace OnlineStore.Services;

public record CouponApplyResult(bool Success, string? Error, decimal DiscountAmount, Coupon? Coupon);

public interface ICouponService
{
    Task<IReadOnlyList<Coupon>> GetAllAsync();

    Task<Coupon?> GetByIdAsync(int id);

    Task<Coupon> CreateAsync(Coupon coupon);

    Task UpdateAsync(Coupon coupon);

    Task<bool> DeleteAsync(int id);

    Task<CouponApplyResult> ValidateAsync(string code, decimal orderTotal);

    Task IncrementUsageAsync(int id);
}
