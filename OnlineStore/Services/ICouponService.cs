using OnlineStore.Models;

namespace OnlineStore.Services;

public record CouponApplyResult(
    bool Success,
    string? Error,
    decimal DiscountAmount,
    Coupon? Coupon,
    bool FreeShipping = false);

public record CouponEvaluationItem(int ProductId, decimal Price, int Quantity);

public enum CouponWalletStatus
{
    Available = 0,
    Scheduled = 1,
    Used = 2,
    Ineligible = 3,
    Expired = 4
}

public record CouponWalletItem(
    Coupon Coupon,
    CouponWalletStatus Status,
    int UserUsage,
    int? RemainingUses);

public interface ICouponService
{
    Task<IReadOnlyList<Coupon>> GetAllAsync();

    Task<Coupon?> GetByIdAsync(int id);

    Task<Coupon> CreateAsync(Coupon coupon, IEnumerable<int>? categoryIds = null, IEnumerable<int>? productIds = null);

    Task UpdateAsync(Coupon coupon, IEnumerable<int>? categoryIds = null, IEnumerable<int>? productIds = null);

    Task<bool> DeleteAsync(int id);

    Task<CouponApplyResult> ValidateAsync(
        string code,
        decimal orderTotal,
        string? userId = null,
        IReadOnlyList<CouponEvaluationItem>? items = null);

    Task<IReadOnlyList<CouponApplyResult>> GetAvailableAsync(
        string userId,
        decimal orderTotal,
        IReadOnlyList<CouponEvaluationItem> items);

    Task<IReadOnlyList<CouponWalletItem>> GetCustomerCouponsAsync(string userId);

    Task IncrementUsageAsync(int id);

    Task<string> GenerateUniqueCodeAsync(string? campaignName);

    Task<int> GenerateBatchAsync(int couponId, int quantity, string? prefix);
}
