using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;

namespace OnlineStore.Services;

public class CouponService : ICouponService
{
    private readonly ApplicationDbContext _context;

    public CouponService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Coupon>> GetAllAsync() =>
        await _context.Coupons
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

    public Task<Coupon?> GetByIdAsync(int id) =>
        _context.Coupons.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Coupon> CreateAsync(Coupon coupon)
    {
        coupon.Code = coupon.Code.Trim().ToUpperInvariant();
        _context.Coupons.Add(coupon);
        await _context.SaveChangesAsync();
        return coupon;
    }

    public async Task UpdateAsync(Coupon coupon)
    {
        coupon.Code = coupon.Code.Trim().ToUpperInvariant();
        _context.Coupons.Update(coupon);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == id);
        if (coupon is null)
        {
            return false;
        }

        _context.Coupons.Remove(coupon);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<CouponApplyResult> ValidateAsync(string code, decimal orderTotal)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var coupon = await _context.Coupons.AsNoTracking().FirstOrDefaultAsync(c => c.Code == normalized);

        if (coupon is null)
        {
            return new CouponApplyResult(false, "优惠码不存在。", 0, null);
        }
        if (!coupon.IsActive)
        {
            return new CouponApplyResult(false, "优惠码已停用。", 0, null);
        }

        var now = DateTime.UtcNow;
        if (now < coupon.StartDate)
        {
            return new CouponApplyResult(false, "优惠码尚未生效。", 0, null);
        }
        if (now > coupon.EndDate)
        {
            return new CouponApplyResult(false, "优惠码已过期。", 0, null);
        }
        if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
        {
            return new CouponApplyResult(false, "优惠码已达使用上限。", 0, null);
        }
        if (coupon.MinOrderAmount.HasValue && orderTotal < coupon.MinOrderAmount.Value)
        {
            return new CouponApplyResult(false, $"订单金额需满 ¥{coupon.MinOrderAmount.Value:F2} 才能使用该优惠码。", 0, null);
        }

        return CouponCalculator.TryCompute(coupon, orderTotal, out var discount)
            ? new CouponApplyResult(true, null, discount, coupon)
            : new CouponApplyResult(false, "优惠码不可用。", 0, null);
    }

    public async Task IncrementUsageAsync(int id)
    {
        var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == id);
        if (coupon is null)
        {
            return;
        }

        coupon.UsedCount++;
        await _context.SaveChangesAsync();
    }
}
