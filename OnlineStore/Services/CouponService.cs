using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using OnlineStore.Data;
using OnlineStore.Models;

namespace OnlineStore.Services;

public class CouponService : ICouponService
{
    private readonly ApplicationDbContext _context;
    private readonly IStringLocalizer<SharedResource> _loc;

    public CouponService(ApplicationDbContext context, IStringLocalizer<SharedResource>? localizer = null)
    {
        _context = context;
        _loc = localizer ?? FallbackStringLocalizer<SharedResource>.Instance;
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
            return new CouponApplyResult(false, _loc["CouponNotFound"].Value, 0, null);
        }
        if (!coupon.IsActive)
        {
            return new CouponApplyResult(false, _loc["CouponDisabled"].Value, 0, null);
        }

        var now = DateTime.UtcNow;
        if (now < coupon.StartDate)
        {
            return new CouponApplyResult(false, _loc["CouponNotStarted"].Value, 0, null);
        }
        if (now > coupon.EndDate)
        {
            return new CouponApplyResult(false, _loc["CouponExpired"].Value, 0, null);
        }
        if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
        {
            return new CouponApplyResult(false, _loc["CouponUsageLimit"].Value, 0, null);
        }
        if (coupon.MinOrderAmount.HasValue && orderTotal < coupon.MinOrderAmount.Value)
        {
            return new CouponApplyResult(false, _loc["CouponMinOrder", coupon.MinOrderAmount.Value.ToString("F2")].Value, 0, null);
        }

        return CouponCalculator.TryCompute(coupon, orderTotal, out var discount)
            ? new CouponApplyResult(true, null, discount, coupon)
            : new CouponApplyResult(false, _loc["CouponUnavailable"].Value, 0, null);
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
