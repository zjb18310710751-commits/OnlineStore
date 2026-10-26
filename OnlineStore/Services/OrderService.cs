using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;

namespace OnlineStore.Services;

public class OrderService : IOrderService
{
    private static readonly TimeSpan TrackingStep = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan DeliveryThreshold = TrackingStep * 3;

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
            .Include(o => o.Items)
            .Include(o => o.AfterSalesRequests)
            .Where(o => o.UserId == userId && o.UserDeletedAt == null)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<Order>> GetAllAsync() =>
        await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.AfterSalesRequests)
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

            ApplyShippingSnapshot(order, products);

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

    public Task<bool> ShipAsync(int id) => ShipAsync(id, "系统默认", $"TEST{id:D8}");

    public async Task<bool> ShipAsync(int id, string? carrier, string? trackingNumber)
    {
        var order = await GetByIdAsync(id);
        if (order is null || order.Status != OrderStatus.PendingShipment)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(carrier) || string.IsNullOrWhiteSpace(trackingNumber))
        {
            return false;
        }

        order.Carrier = carrier.Trim();
        order.TrackingNumber = trackingNumber.Trim();
        order.ShippedAt = DateTime.UtcNow;
        order.DeliveredAt = null;
        order.SignedAt = null;
        order.Status = OrderStatus.Shipped;
        await _context.SaveChangesAsync();
        return true;
    }

    public Task<bool> CompleteAsync(int id) =>
        TransitionAsync(id, OrderStatus.Completed, false, OrderStatus.Shipped);

    public async Task<OrderTrackingView?> GetTrackingAsync(int id)
    {
        var order = await GetByIdAsync(id);
        if (order is null || order.Status == OrderStatus.PendingShipment || order.ShippedAt is null)
        {
            return null;
        }

        await AdvanceDeliveryAsync(order);

        var now = DateTime.UtcNow;
        var shippedAt = order.ShippedAt!.Value;
        var deliveredAt = shippedAt.Add(DeliveryThreshold);
        var signed = order.Status == OrderStatus.Completed || order.SignedAt.HasValue;
        var deliveredReached = order.DeliveredAt.HasValue || now >= deliveredAt;
        var origin = !string.IsNullOrWhiteSpace(order.ShippingOrigin)
            ? L10n.Text(order.ShippingOrigin, order.ShippingOriginEn)
            : L10n.Text("广东深圳", "Shenzhen, China");
        var destination = string.IsNullOrWhiteSpace(order.Address)
            ? L10n.Text("顾客填写的收货地址", "the recipient's delivery address")
            : order.Address;

        var nodes = new List<OrderTrackingNode>
        {
            Node(1,
                L10n.Text("订单已包装", "Order packaged"),
                $"{L10n.Text("商家已在", "Packaged at")} {origin} {L10n.Text("完成包装与出库检查。", "and passed dispatch checks.")}",
                order.CreatedAt, true, false),
            Node(2,
                L10n.Text("已交给承运商", "Handed to carrier"),
                $"{L10n.Text("包裹已从", "Parcel departed")} {origin} {L10n.Text("交给", "with")} {L10n.Carrier(order.Carrier)} ({order.TrackingNumber})",
                shippedAt, true, false),
            Node(3,
                L10n.Text("已离开分拨中心", "Departed regional hub"),
                $"{L10n.Text("包裹正在发往", "The parcel is travelling to")} {destination}",
                shippedAt.Add(TrackingStep), now >= shippedAt.Add(TrackingStep), !deliveredReached && now >= shippedAt.Add(TrackingStep) && now < shippedAt.Add(TrackingStep * 2)),
            Node(4,
                L10n.Text("已到达派送网点", "Arrived at local depot"),
                $"{L10n.Text("快递员正在派送至", "The courier is delivering to")} {destination}",
                shippedAt.Add(TrackingStep * 2), now >= shippedAt.Add(TrackingStep * 2), !deliveredReached && now >= shippedAt.Add(TrackingStep * 2) && now < deliveredAt),
            Node(5,
                signed
                    ? L10n.Text("已签收", "Delivered & signed")
                    : L10n.Text("已送达，等待签收", "Delivered, awaiting confirmation"),
                signed
                    ? $"{L10n.Text("买家已在", "Delivered to")} {destination} {L10n.Text("确认收货，订单完成。", "and receipt was confirmed.")}"
                    : $"{L10n.Text("包裹已送达", "Delivered to")} {destination}，{L10n.Text("等待买家确认收货。", "awaiting buyer confirmation.")}",
                signed ? order.SignedAt : (deliveredReached ? deliveredAt : null),
                signed || deliveredReached,
                signed ? false : deliveredReached,
                !signed && !deliveredReached)
        };

        return new OrderTrackingView
        {
            OrderId = order.Id,
            OrderNo = order.OrderNo,
            Carrier = L10n.Carrier(order.Carrier),
            TrackingNumber = order.TrackingNumber ?? string.Empty,
            OfficialTrackingUrl = $"https://t.17track.net/en#nums={Uri.EscapeDataString(order.TrackingNumber ?? string.Empty)}",
            OriginLocation = origin,
            DestinationCity = destination,
            DestinationPostalCode = string.Empty,
            RecipientName = order.UserName,
            Status = order.Status,
            ShippedAt = shippedAt,
            CompletedAt = order.SignedAt,
            Nodes = nodes
        };
    }

    public async Task<bool> ConfirmReceiptAsync(int id, string userId)
    {
        var order = await GetByIdAsync(id);
        if (order is null || !string.Equals(order.UserId, userId, StringComparison.Ordinal))
        {
            return false;
        }

        if (order.Status != OrderStatus.Shipped && order.Status != OrderStatus.Delivered)
        {
            return false;
        }

        await AdvanceDeliveryAsync(order);
        order.Status = OrderStatus.Completed;
        order.SignedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteForUserAsync(int id, string userId)
    {
        var order = await GetByIdAsync(id);
        if (order is null || !string.Equals(order.UserId, userId, StringComparison.Ordinal)
            || order.UserDeletedAt.HasValue)
        {
            return false;
        }

        if (order.Status != OrderStatus.Completed
            && order.Status != OrderStatus.Cancelled
            && order.Status != OrderStatus.Refunded)
        {
            return false;
        }

        var hasOpenAfterSales = await _context.AfterSalesRequests.AnyAsync(a =>
            a.OrderId == id && a.Status != AfterSalesStatus.Completed && a.Status != AfterSalesStatus.Rejected);
        if (hasOpenAfterSales)
        {
            return false;
        }

        order.UserDeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetDeliveredItemCountAsync(string userId)
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId
                && o.UserDeletedAt == null
                && (o.Status == OrderStatus.Shipped || o.Status == OrderStatus.Delivered))
            .ToListAsync();

        foreach (var order in orders.Where(o => o.Status == OrderStatus.Shipped))
        {
            await AdvanceDeliveryAsync(order);
        }

        return orders
            .Where(o => o.Status == OrderStatus.Delivered)
            .Sum(o => o.Items.Sum(i => i.Quantity));
    }

    public async Task<AfterSalesRequest> CreateAfterSalesAsync(int orderId, string userId, string type, string reason)
    {
        var order = await GetByIdAsync(orderId);
        if (order is null || !string.Equals(order.UserId, userId, StringComparison.Ordinal) || order.Status != OrderStatus.Completed)
        {
            throw new InvalidOperationException(L10n.Text("当前订单不能申请售后。", "This order is not eligible for after-sales."));
        }

        var normalizedType = type == AfterSalesType.Exchange ? AfterSalesType.Exchange : AfterSalesType.Return;
        var request = new AfterSalesRequest
        {
            OrderId = order.Id,
            UserId = userId,
            Type = normalizedType,
            Reason = reason.Trim(),
            Status = AfterSalesStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };
        _context.AfterSalesRequests.Add(request);
        await _context.SaveChangesAsync();
        return request;
    }

    public async Task<IReadOnlyList<AfterSalesRequest>> GetAfterSalesByUserAsync(string userId) =>
        await _context.AfterSalesRequests
            .AsNoTracking()
            .Include(a => a.Order)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.RequestedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<AfterSalesRequest>> GetAfterSalesAllAsync() =>
        await _context.AfterSalesRequests
            .AsNoTracking()
            .Include(a => a.Order)
            .OrderByDescending(a => a.RequestedAt)
            .ToListAsync();

    public async Task<bool> ReviewAfterSalesAsync(int id, bool approved, string? note)
    {
        var request = await _context.AfterSalesRequests.FirstOrDefaultAsync(a => a.Id == id);
        if (request is null || request.Status != AfterSalesStatus.Pending)
        {
            return false;
        }

        request.Status = approved ? AfterSalesStatus.Approved : AfterSalesStatus.Rejected;
        request.AdminNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        request.ReviewedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAfterSalesReceivedAsync(int id)
    {
        var request = await _context.AfterSalesRequests.FirstOrDefaultAsync(a => a.Id == id);
        if (request is null || request.Status != AfterSalesStatus.Approved)
        {
            return false;
        }

        request.Status = AfterSalesStatus.Received;
        request.ReceivedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CompleteAfterSalesAsync(int id)
    {
        var request = await _context.AfterSalesRequests
            .Include(a => a.Order)
            .ThenInclude(o => o!.Items)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (request is null || request.Status != AfterSalesStatus.Received)
        {
            return false;
        }

        if (request.Order is not null && request.Type == AfterSalesType.Return)
        {
            await RestoreStockAsync(request.Order);
        }

        request.Status = AfterSalesStatus.Completed;
        request.CompletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public Task<int> CountAsync() => _context.Orders.CountAsync();

    private async Task AdvanceDeliveryAsync(Order order)
    {
        if (order.Status != OrderStatus.Shipped || order.ShippedAt is null)
        {
            return;
        }

        if (DateTime.UtcNow < order.ShippedAt.Value.Add(DeliveryThreshold))
        {
            return;
        }

        order.Status = OrderStatus.Delivered;
        order.DeliveredAt = order.ShippedAt.Value.Add(DeliveryThreshold);
        await _context.SaveChangesAsync();
    }

    private static OrderTrackingNode Node(
        int step,
        string title,
        string description,
        DateTime? time,
        bool isReached,
        bool isCurrentActive,
        bool isFutureEstimated = false) =>
        new()
        {
            StepIndex = step,
            Title = title,
            Description = description,
            FormattedTime = time.HasValue ? time.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") : null,
            IsReached = isReached,
            IsCurrentActive = isCurrentActive,
            IsFutureEstimated = isFutureEstimated
        };

    private static string DestinationPart(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return string.Empty;
        }

        return address.Split(',').Select(part => part.Trim()).FirstOrDefault(part => part.Length > 0) ?? string.Empty;
    }

    private static void ApplyShippingSnapshot(Order order, IReadOnlyList<Product> products)
    {
        if (products.Count == 0)
        {
            return;
        }

        order.ShippingOrigin = JoinDistinct(products.Select(p => p.ShippingFrom));
        order.ShippingOriginEn = JoinDistinct(products.Select(p => p.ShippingFromEn));
        order.ShippingFeeText = JoinDistinct(products.Select(p => p.ShippingFee));
        order.ShippingFeeTextEn = JoinDistinct(products.Select(p => p.ShippingFeeEn));
        order.DispatchTimeText = JoinDistinct(products.Select(p => p.DeliveryTime));
        order.DispatchTimeTextEn = JoinDistinct(products.Select(p => p.DeliveryTimeEn));

        var dispatchHours = products
            .Select(p => ParseDispatchHours(p.DeliveryTime))
            .DefaultIfEmpty(72)
            .Max();
        order.ExpectedDispatchAt = DateTime.UtcNow.AddHours(dispatchHours);
    }

    private static string? JoinDistinct(IEnumerable<string?> values) =>
        string.Join("、", values.Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!.Trim())
            .Distinct());

    private static int ParseDispatchHours(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 72;
        }

        var match = Regex.Match(text, @"\d+");
        return match.Success && int.TryParse(match.Value, out var hours) ? hours : 72;
    }

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
