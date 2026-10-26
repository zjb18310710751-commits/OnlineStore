using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;

namespace OnlineStore.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly ICouponService _couponService;

    public OrderService(ApplicationDbContext context, ICouponService? couponService = null)
    {
        _context = context;
        _couponService = couponService ?? new CouponService(context);
    }

    public Task<Order?> GetByIdAsync(int id) =>
        _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<IReadOnlyList<Order>> GetByUserAsync(string userId) =>
        await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<Order>> GetAllAsync() =>
        await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public async Task<Order> CreateAsync(Order order)
    {
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        try
        {
            if (order.Items.Count == 0 || order.Items.Any(i => i.Quantity <= 0))
            {
                throw new OrderValidationException("CheckoutQuantityInvalid");
            }

            var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
            foreach (var product in products)
            {
                await _context.Entry(product).ReloadAsync();
            }

            var skuIds = order.Items.Where(i => i.SkuId.HasValue).Select(i => i.SkuId!.Value).Distinct().ToList();
            var skus = await _context.ProductSkus.Where(s => skuIds.Contains(s.Id)).ToListAsync();
            foreach (var sku in skus)
            {
                await _context.Entry(sku).ReloadAsync();
            }

            foreach (var item in order.Items)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                var sku = item.SkuId.HasValue ? skus.FirstOrDefault(s => s.Id == item.SkuId) : null;
                if (product is null || !product.IsActive || _context.Entry(product).State == EntityState.Detached
                    || (item.SkuId.HasValue && (sku is null || !sku.IsActive || sku.ProductId != product.Id
                        || _context.Entry(sku).State == EntityState.Detached)))
                {
                    throw new OrderValidationException("CheckoutCartUnavailable");
                }

                var price = sku?.Price ?? product.Price;
                if (price < 0 || item.Price != price)
                {
                    throw new OrderValidationException("CheckoutPriceChanged");
                }

                item.ProductName = product.Name;
                item.ProductNameEn = product.NameEn;
                item.Color = sku?.Color;
                item.ColorEn = sku?.ColorEn;
                item.Spec = sku?.Spec;
                item.SpecEn = sku?.SpecEn;
                item.Price = price;
            }

            var productQuantities = order.Items.GroupBy(i => i.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(i => (long)i.Quantity));
            var skuQuantities = order.Items.Where(i => i.SkuId.HasValue).GroupBy(i => i.SkuId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(i => (long)i.Quantity));
            if (products.Any(p => productQuantities[p.Id] > p.Stock)
                || skus.Any(s => skuQuantities[s.Id] > s.Stock))
            {
                throw new OrderValidationException("CheckoutStockUnavailable");
            }

            var subtotal = order.Items.Sum(i => i.Price * i.Quantity);
            var coupon = await GetTrackedCouponAsync(order);
            var discount = 0m;
            var freeShipping = false;
            if (coupon is not null)
            {
                var result = await _couponService.ValidateAsync(
                    coupon.Code,
                    subtotal,
                    order.UserId,
                    ToEvaluationItems(order));
                if (!result.Success || result.Coupon is null)
                {
                    throw new OrderValidationException("CouponUnavailable");
                }

                discount = result.DiscountAmount;
                freeShipping = result.FreeShipping;
                if (discount < order.DiscountAmount)
                {
                    throw new OrderValidationException("CouponUnavailable");
                }
            }

            foreach (var product in products)
            {
                product.Stock -= (int)productQuantities[product.Id];
            }
            foreach (var sku in skus)
            {
                sku.Stock -= (int)skuQuantities[sku.Id];
            }
            if (coupon is not null)
            {
                coupon.UsedCount++;
            }

            order.Total = subtotal - discount;
            order.DiscountAmount = discount;
            order.CouponCode = coupon?.Code;
            order.CouponName = coupon?.CampaignName;
            order.Status = OrderStatus.PendingShipment;
            order.CreatedAt = DateTime.UtcNow;
            _context.Orders.Add(order);
            if (coupon is not null)
            {
                _context.CouponRedemptions.Add(new CouponRedemption
                {
                    Coupon = coupon,
                    UserId = order.UserId ?? string.Empty,
                    Order = order,
                    Code = coupon.Code,
                    DiscountAmount = discount,
                    ShippingWaived = freeShipping,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
            if (transaction is not null)
            {
                await transaction.CommitAsync();
            }
            return order;
        }
        catch (Exception exception) when (IsConcurrencyFailure(exception))
        {
            _context.ChangeTracker.Clear();
            throw new OrderValidationException("CheckoutRetry", exception);
        }
        catch
        {
            _context.ChangeTracker.Clear();
            throw;
        }
    }

    public Task<bool> CancelAsync(int id) =>
        TransitionAsync(id, OrderStatus.Cancelled, true, OrderStatus.PendingShipment);

    public Task<bool> RefundAsync(int id) =>
        TransitionAsync(id, OrderStatus.Refunded, true, OrderStatus.Shipped, OrderStatus.Completed);

    public Task<bool> ShipAsync(int id) =>
        TransitionAsync(id, OrderStatus.Shipped, false, OrderStatus.PendingShipment);

    public Task<bool> CompleteAsync(int id) =>
        TransitionAsync(id, OrderStatus.Completed, false, OrderStatus.Shipped);

    public Task<int> CountAsync() => _context.Orders.CountAsync();

    private async Task<Coupon?> GetTrackedCouponAsync(Order order)
    {
        if (string.IsNullOrWhiteSpace(order.CouponCode))
        {
            return null;
        }

        var normalized = order.CouponCode.Trim().ToUpperInvariant();
        var coupon = await _context.Coupons
            .Include(c => c.Categories)
            .Include(c => c.Products)
            .SingleOrDefaultAsync(c => c.Code == normalized && !c.IsDeleted);
        if (coupon is not null)
        {
            await _context.Entry(coupon).ReloadAsync();
        }

        if (coupon is null || _context.Entry(coupon).State == EntityState.Detached)
        {
            throw new OrderValidationException("CouponUnavailable");
        }

        return coupon;
    }

    private async Task<bool> TransitionAsync(int id, string nextStatus, bool restoreStock, params string[] allowedStatuses)
    {
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        try
        {
            var order = await GetByIdAsync(id);
            if (order is null)
            {
                return false;
            }
            await _context.Entry(order).ReloadAsync();
            if (_context.Entry(order).State == EntityState.Detached || !allowedStatuses.Contains(order.Status))
            {
                return false;
            }
            if (restoreStock)
            {
                await RestoreStockAsync(order);
                await ReleaseCouponAsync(order);
            }
            order.Status = nextStatus;
            await _context.SaveChangesAsync();
            if (transaction is not null)
            {
                await transaction.CommitAsync();
            }
            return true;
        }
        catch (Exception exception) when (IsConcurrencyFailure(exception) || exception is OverflowException)
        {
            _context.ChangeTracker.Clear();
            return false;
        }
    }

    private async Task RestoreStockAsync(Order order)
    {
        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        if (productIds.Count == 0)
        {
            return;
        }

        var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
        foreach (var product in products)
        {
            await _context.Entry(product).ReloadAsync();
            if (_context.Entry(product).State != EntityState.Detached)
            {
                product.Stock = checked(product.Stock + (int)order.Items.Where(i => i.ProductId == product.Id && i.Quantity > 0)
                    .Sum(i => (long)i.Quantity));
            }
        }

        var skuIds = order.Items.Where(i => i.SkuId.HasValue).Select(i => i.SkuId!.Value).Distinct().ToList();
        var skus = await _context.ProductSkus.Where(s => skuIds.Contains(s.Id)).ToListAsync();
        foreach (var sku in skus)
        {
            await _context.Entry(sku).ReloadAsync();
            if (_context.Entry(sku).State != EntityState.Detached)
            {
                sku.Stock = checked(sku.Stock + (int)order.Items.Where(i => i.SkuId == sku.Id && i.ProductId == sku.ProductId && i.Quantity > 0)
                    .Sum(i => (long)i.Quantity));
            }
        }
    }

    private async Task ReleaseCouponAsync(Order order)
    {
        var redemption = await _context.CouponRedemptions
            .Include(r => r.Coupon)
            .FirstOrDefaultAsync(r => r.OrderId == order.Id && r.ReleasedAt == null);
        if (redemption is null)
        {
            return;
        }

        redemption.ReleasedAt = DateTime.UtcNow;
        if (redemption.Coupon is not null && redemption.Coupon.UsedCount > 0)
        {
            redemption.Coupon.UsedCount--;
        }
    }

    private static IReadOnlyList<CouponEvaluationItem> ToEvaluationItems(Order order) =>
        order.Items
            .Select(item => new CouponEvaluationItem(item.ProductId, item.Price, item.Quantity))
            .ToList();

    private static bool IsConcurrencyFailure(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbUpdateConcurrencyException || current is SqlException { Number: 1205 })
            {
                return true;
            }
        }
        return false;
    }
}
