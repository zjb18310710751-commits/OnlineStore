using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using OnlineStore.Data;
using OnlineStore.Models;

namespace OnlineStore.Services;

public class CouponService : ICouponService
{
    private const string BatchCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly ApplicationDbContext _context;
    private readonly IStringLocalizer<SharedResource> _loc;

    public CouponService(ApplicationDbContext context, IStringLocalizer<SharedResource>? localizer = null)
    {
        _context = context;
        _loc = localizer ?? FallbackStringLocalizer<SharedResource>.Instance;
    }

    public async Task<IReadOnlyList<Coupon>> GetAllAsync() =>
        await CouponQuery()
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

    public Task<Coupon?> GetByIdAsync(int id) =>
        CouponQuery().FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

    public async Task<Coupon> CreateAsync(
        Coupon coupon,
        IEnumerable<int>? categoryIds = null,
        IEnumerable<int>? productIds = null)
    {
        coupon.Code = coupon.Code.Trim().ToUpperInvariant();
        coupon.CampaignName = NormalizeOptional(coupon.CampaignName);
        await SetScopesAsync(coupon, categoryIds, productIds);
        _context.Coupons.Add(coupon);
        await _context.SaveChangesAsync();
        return coupon;
    }

    public async Task UpdateAsync(
        Coupon coupon,
        IEnumerable<int>? categoryIds = null,
        IEnumerable<int>? productIds = null)
    {
        coupon.Code = coupon.Code.Trim().ToUpperInvariant();
        coupon.CampaignName = NormalizeOptional(coupon.CampaignName);
        await SetScopesAsync(coupon, categoryIds, productIds);
        _context.Coupons.Update(coupon);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var coupon = await CouponQuery().FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (coupon is null)
        {
            return false;
        }

        if (await _context.CouponRedemptions.AnyAsync(r => r.CouponId == id))
        {
            coupon.IsDeleted = true;
            coupon.IsActive = false;
        }
        else
        {
            _context.Coupons.Remove(coupon);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<CouponApplyResult> ValidateAsync(
        string code,
        decimal orderTotal,
        string? userId = null,
        IReadOnlyList<CouponEvaluationItem>? items = null)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var coupon = await CouponQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == normalized && !c.IsDeleted);
        if (coupon is null)
        {
            return Failure("CouponNotFound");
        }

        var context = await BuildEvaluationContextAsync(userId, orderTotal, items);
        return Evaluate(coupon, context);
    }

    public async Task<IReadOnlyList<CouponApplyResult>> GetAvailableAsync(
        string userId,
        decimal orderTotal,
        IReadOnlyList<CouponEvaluationItem> items)
    {
        var coupons = await CouponQuery()
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .ToListAsync();
        var context = await BuildEvaluationContextAsync(userId, orderTotal, items);

        return coupons
            .Select(coupon => Evaluate(coupon, context))
            .Where(result => result.Success)
            .OrderByDescending(result => result.DiscountAmount)
            .ThenByDescending(result => result.Coupon?.DiscountType == CouponDiscountType.FreeShipping)
            .ThenBy(result => result.Coupon?.EndDate)
            .ToList();
    }

    public async Task<IReadOnlyList<CouponWalletItem>> GetCustomerCouponsAsync(string userId)
    {
        var coupons = await CouponQuery()
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.IsActive)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        var usage = await _context.CouponRedemptions
            .AsNoTracking()
            .Where(redemption => redemption.UserId == userId && redemption.ReleasedAt == null)
            .GroupBy(redemption => redemption.CouponId)
            .Select(group => new { CouponId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.CouponId, row => row.Count);
        var hasPreviousOrder = await _context.Orders
            .AsNoTracking()
            .AnyAsync(order => order.UserId == userId);
        var now = DateTime.UtcNow;

        return coupons
            .Select(coupon =>
            {
                var userUsage = usage.GetValueOrDefault(coupon.Id);
                var remaining = coupon.PerUserLimit.HasValue
                    ? Math.Max(0, coupon.PerUserLimit.Value - userUsage)
                    : null as int?;
                var status = coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value
                    || coupon.PerUserLimit.HasValue && userUsage >= coupon.PerUserLimit.Value
                    ? CouponWalletStatus.Used
                    : now < coupon.StartDate
                        ? CouponWalletStatus.Scheduled
                        : now > coupon.EndDate
                            ? CouponWalletStatus.Expired
                            : coupon.FirstOrderOnly && hasPreviousOrder
                                ? CouponWalletStatus.Ineligible
                                : CouponWalletStatus.Available;

                return new CouponWalletItem(coupon, status, userUsage, remaining);
            })
            .OrderBy(item => item.Status)
            .ThenBy(item => item.Coupon.EndDate)
            .ToList();
    }

    public async Task<string> GenerateUniqueCodeAsync(string? campaignName)
    {
        var prefix = NormalizeCodePrefix(campaignName, "ZHY");
        var existingCodes = (await _context.Coupons
            .Where(c => c.Code.StartsWith(prefix + "-"))
            .Select(c => c.Code)
            .ToListAsync())
            .ToHashSet();

        string code;
        do
        {
            code = $"{prefix}-{RandomNumberGenerator.GetString(BatchCharacters, 6)}";
        }
        while (existingCodes.Contains(code));

        return code;
    }

    public async Task<int> GenerateBatchAsync(int couponId, int quantity, string? prefix)
    {
        if (quantity is < 1 or > 50)
        {
            throw new InvalidOperationException(_loc["CouponBatchQuantityInvalid"].Value);
        }

        var source = await CouponQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == couponId && !c.IsDeleted);
        if (source is null)
        {
            throw new InvalidOperationException(_loc["CouponNotFound"].Value);
        }

        var normalizedPrefix = NormalizeBatchPrefix(prefix, source.CampaignName);
        var existingCodes = (await _context.Coupons
            .Where(c => c.Code.StartsWith(normalizedPrefix + "-"))
            .Select(c => c.Code)
            .ToListAsync())
            .ToHashSet();

        var generated = new List<Coupon>(quantity);
        for (var i = 0; i < quantity; i++)
        {
            string code;
            do
            {
                code = $"{normalizedPrefix}-{RandomNumberGenerator.GetString(BatchCharacters, 6)}";
            }
            while (existingCodes.Contains(code));

            existingCodes.Add(code);
            generated.Add(CloneCoupon(source, code, i + 1));
        }

        _context.Coupons.AddRange(generated);
        await _context.SaveChangesAsync();
        return generated.Count;
    }

    private IQueryable<Coupon> CouponQuery() =>
        _context.Coupons
            .Include(c => c.Categories)
            .Include(c => c.Products);

    private async Task SetScopesAsync(
        Coupon coupon,
        IEnumerable<int>? categoryIds,
        IEnumerable<int>? productIds)
    {
        var selectedCategories = (categoryIds ?? Array.Empty<int>()).Distinct().ToList();
        var selectedProducts = (productIds ?? Array.Empty<int>()).Distinct().ToList();

        coupon.Categories.Clear();
        coupon.Products.Clear();

        if (coupon.Scope == CouponScope.Categories && selectedCategories.Count > 0)
        {
            var categories = await _context.Categories
                .Where(c => selectedCategories.Contains(c.Id))
                .ToListAsync();
            foreach (var category in categories)
            {
                coupon.Categories.Add(category);
            }
        }

        if (coupon.Scope == CouponScope.Products && selectedProducts.Count > 0)
        {
            var products = await _context.Products
                .Where(p => selectedProducts.Contains(p.Id))
                .ToListAsync();
            foreach (var product in products)
            {
                coupon.Products.Add(product);
            }
        }
    }

    private CouponApplyResult Evaluate(Coupon coupon, CouponEvaluationContext context)
    {
        if (!coupon.IsActive)
        {
            return Failure("CouponDisabled");
        }

        var now = DateTime.UtcNow;
        if (now < coupon.StartDate)
        {
            return Failure("CouponNotStarted");
        }
        if (now > coupon.EndDate)
        {
            return Failure("CouponExpired");
        }
        if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
        {
            return Failure("CouponUsageLimit");
        }

        if (!string.IsNullOrWhiteSpace(context.UserId))
        {
            var usedByUser = context.RedemptionsByCoupon.GetValueOrDefault(coupon.Id);
            if (coupon.PerUserLimit.HasValue && usedByUser >= coupon.PerUserLimit.Value)
            {
                return Failure("CouponPerUserLimit");
            }

            if (coupon.FirstOrderOnly && context.HasPreviousOrder)
            {
                return Failure("CouponFirstOrderOnly");
            }
        }

        var eligibleSubtotal = CalculateEligibleSubtotal(coupon, context);
        if (eligibleSubtotal is null)
        {
            return Failure("CouponItemsNotEligible");
        }

        if (coupon.MinOrderAmount.HasValue && eligibleSubtotal.Value < coupon.MinOrderAmount.Value)
        {
            return new CouponApplyResult(
                false,
                _loc["CouponMinOrder", coupon.MinOrderAmount.Value.ToString("F2")].Value,
                0,
                null);
        }

        if (!CouponCalculator.TryCompute(coupon, eligibleSubtotal.Value, out var discount, out var freeShipping))
        {
            return Failure("CouponUnavailable");
        }

        return new CouponApplyResult(true, null, discount, coupon, freeShipping);
    }

    private static decimal? CalculateEligibleSubtotal(Coupon coupon, CouponEvaluationContext context)
    {
        if (context.Items.Count == 0)
        {
            return coupon.Scope == CouponScope.AllProducts && !coupon.ExcludeDiscountedItems
                ? context.OrderTotal
                : null;
        }

        decimal subtotal = 0;
        var selectedCategories = coupon.Categories.Select(c => c.Id).ToHashSet();
        var selectedProducts = coupon.Products.Select(p => p.Id).ToHashSet();

        foreach (var item in context.Items)
        {
            if (!context.Products.TryGetValue(item.ProductId, out var product))
            {
                continue;
            }

            if (coupon.ExcludeDiscountedItems && product.IsOnSale)
            {
                continue;
            }

            var matches = coupon.Scope switch
            {
                CouponScope.AllProducts => true,
                CouponScope.Categories => product.CategoryIds.Any(selectedCategories.Contains),
                CouponScope.Products => selectedProducts.Contains(item.ProductId),
                _ => false
            };

            if (matches)
            {
                subtotal += item.Price * item.Quantity;
            }
        }

        return subtotal > 0 ? subtotal : null;
    }

    private async Task<CouponEvaluationContext> BuildEvaluationContextAsync(
        string? userId,
        decimal orderTotal,
        IReadOnlyList<CouponEvaluationItem>? items)
    {
        var normalizedItems = items?
            .Where(item => item.Quantity > 0)
            .ToList() ?? new List<CouponEvaluationItem>();
        var productIds = normalizedItems.Select(item => item.ProductId).Distinct().ToList();
        var products = productIds.Count == 0
            ? new Dictionary<int, CouponProductInfo>()
            : await _context.Products
                .AsNoTracking()
                .Where(product => productIds.Contains(product.Id))
                .Select(product => new CouponProductInfo(
                    product.Id,
                    product.OriginalPrice.HasValue && product.OriginalPrice.Value > product.Price,
                    product.Categories.Select(category => category.Id).ToList()))
                .ToDictionaryAsync(product => product.Id);

        var redemptions = string.IsNullOrWhiteSpace(userId)
            ? new Dictionary<int, int>()
            : await _context.CouponRedemptions
                .AsNoTracking()
                .Where(redemption => redemption.UserId == userId && redemption.ReleasedAt == null)
                .GroupBy(redemption => redemption.CouponId)
                .Select(group => new { CouponId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(row => row.CouponId, row => row.Count);

        var hasPreviousOrder = !string.IsNullOrWhiteSpace(userId)
            && await _context.Orders.AsNoTracking().AnyAsync(order => order.UserId == userId);

        return new CouponEvaluationContext(
            userId,
            orderTotal,
            normalizedItems,
            products,
            redemptions,
            hasPreviousOrder);
    }

    private static Coupon CloneCoupon(Coupon source, string code, int batchIndex) => new()
    {
        Code = code,
        CampaignName = $"{source.CampaignName ?? "Coupon"} #{batchIndex}",
        DiscountType = source.DiscountType,
        DiscountValue = source.DiscountValue,
        MaxDiscountAmount = source.MaxDiscountAmount,
        MinOrderAmount = source.MinOrderAmount,
        StartDate = source.StartDate,
        EndDate = source.EndDate,
        IsActive = source.IsActive,
        UsageLimit = source.UsageLimit,
        PerUserLimit = source.PerUserLimit,
        FirstOrderOnly = source.FirstOrderOnly,
        ExcludeDiscountedItems = source.ExcludeDiscountedItems,
        Scope = source.Scope,
        CreatedAt = DateTime.UtcNow,
        Categories = source.Categories.ToList(),
        Products = source.Products.ToList()
    };

    private static string NormalizeBatchPrefix(string? prefix, string? sourceName)
    {
        var source = string.IsNullOrWhiteSpace(prefix) ? sourceName : prefix;
        return NormalizeCodePrefix(source, "ZHY");
    }

    private static string NormalizeCodePrefix(string? source, string fallback)
    {
        var normalized = new string((source ?? string.Empty)
            .Trim()
            .ToUpperInvariant()
            .Where(character => character is >= 'A' and <= 'Z'
                or >= '0' and <= '9'
                or '-')
            .Take(30)
            .ToArray())
            .Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private CouponApplyResult Failure(string resourceKey) =>
        new(false, _loc[resourceKey].Value, 0, null);

    private sealed record CouponProductInfo(int Id, bool IsOnSale, IReadOnlyList<int> CategoryIds);

    private sealed record CouponEvaluationContext(
        string? UserId,
        decimal OrderTotal,
        IReadOnlyList<CouponEvaluationItem> Items,
        IReadOnlyDictionary<int, CouponProductInfo> Products,
        IReadOnlyDictionary<int, int> RedemptionsByCoupon,
        bool HasPreviousOrder);
}
