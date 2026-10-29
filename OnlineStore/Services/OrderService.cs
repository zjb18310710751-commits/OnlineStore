using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.ViewModels;

namespace OnlineStore.Services;

public class OrderService : IOrderService
{
    private static readonly TimeSpan TrackingStep = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan DeliveryThreshold = TrackingStep * 3;
    private static readonly TimeSpan AutoConfirmationWindow = TimeSpan.FromDays(7);

    private readonly ApplicationDbContext _context;
    private readonly ICouponService _couponService;
    private readonly IOrderNumberGenerator _orderNumberGenerator;
    private readonly TimeProvider _timeProvider;

    public OrderService(
        ApplicationDbContext context,
        ICouponService? couponService = null,
        IOrderNumberGenerator? orderNumberGenerator = null,
        TimeProvider? timeProvider = null)
    {
        _context = context;
        _couponService = couponService ?? new CouponService(context);
        _timeProvider = timeProvider ?? TimeProvider.System;
        _orderNumberGenerator = orderNumberGenerator ?? new OrderNumberGenerator(_timeProvider);
    }

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    public Task<Order?> GetByIdAsync(int id) =>
        _context.Orders
            .Include(o => o.Items)
            .Include(o => o.AfterSalesRequests)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<IReadOnlyList<Order>> GetByUserAsync(string userId)
    {
        await ProcessPendingOrderStatesAsync(userId);
        return await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.AfterSalesRequests)
            .Where(o => o.UserId == userId && o.UserDeletedAt == null)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<CustomerOrderUpdateViewModel>> GetCustomerOrderUpdatesAsync(
        string userId,
        IReadOnlyCollection<int> orderIds)
    {
        await ProcessPendingOrderStatesAsync(userId);
        var query = _context.Orders
            .AsNoTracking()
            .Include(order => order.AfterSalesRequests)
            .Where(order => order.UserId == userId && order.UserDeletedAt == null);
        if (orderIds.Count > 0)
        {
            query = query.Where(order => orderIds.Contains(order.Id));
        }

        var orders = await query.ToListAsync();
        return orders
            .Select(CustomerOrderUpdateViewModel.FromOrder)
            .ToList();
    }

    public async Task<IReadOnlyList<Order>> GetAllAsync() =>
        await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.AfterSalesRequests)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyDictionary<int, OrderItemPreviewViewModel>> GetOrderItemPreviewsAsync(IEnumerable<OrderItem> items)
    {
        var orderItems = items.ToArray();
        if (orderItems.Length == 0) return new Dictionary<int, OrderItemPreviewViewModel>();

        var productIds = orderItems.Select(item => item.ProductId).Distinct().ToArray();
        var products = await _context.Products.AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .Select(product => new
            {
                product.Id,
                product.ImageUrl,
                product.IsActive,
                GalleryImage = product.Images.Where(image => !string.IsNullOrWhiteSpace(image.ImageUrl))
                    .OrderBy(image => image.SortOrder).ThenBy(image => image.Id)
                    .Select(image => image.ImageUrl).FirstOrDefault()
            }).ToDictionaryAsync(product => product.Id);
        var skuIds = orderItems.Where(item => item.SkuId.HasValue).Select(item => item.SkuId!.Value).Distinct().ToArray();
        var skus = await _context.ProductSkus.AsNoTracking()
            .Where(sku => skuIds.Contains(sku.Id) && productIds.Contains(sku.ProductId))
            .Select(sku => new { sku.Id, sku.ProductId, sku.ImageUrl })
            .ToDictionaryAsync(sku => sku.Id);

        var previews = new Dictionary<int, OrderItemPreviewViewModel>();
        foreach (var item in orderItems)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
            {
                previews[item.Id] = new(null, false);
                continue;
            }

            var image = !string.IsNullOrWhiteSpace(product.ImageUrl) ? product.ImageUrl : product.GalleryImage;
            if (item.SkuId.HasValue && skus.TryGetValue(item.SkuId.Value, out var sku)
                && sku.ProductId == item.ProductId && !string.IsNullOrWhiteSpace(sku.ImageUrl))
            {
                image = sku.ImageUrl;
            }
            previews[item.Id] = new(image, product.IsActive);
        }
        return previews;
    }

    public async Task<PagedResult<Order>> GetAdminOrdersPagedAsync(int page, int pageSize)
    {
        await ProcessPendingOrderStatesAsync();
        var query = _context.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .Include(order => order.AfterSalesRequests)
            .OrderByDescending(order => order.CreatedAt);
        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<Order>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<Order>> GetAdminOrderUpdatesAsync(IReadOnlyCollection<int> orderIds)
    {
        await ProcessPendingOrderStatesAsync();
        return await _context.Orders.AsNoTracking()
            .Include(order => order.AfterSalesRequests)
            .Where(order => orderIds.Contains(order.Id))
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync();
    }

    public async Task<Order> CreateAsync(Order order)
    {
        try
        {
            return await ExecuteInTransactionAsync(() => CreateOrderCoreAsync(order));
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

    private async Task<Order> CreateOrderCoreAsync(Order order)
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
                item.ImageUrl = string.IsNullOrWhiteSpace(sku?.ImageUrl) ? product.ImageUrl : sku.ImageUrl;
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

            var shippingAmount = CalculateShippingAmount(products, freeShipping);
            order.ShippingAmount = shippingAmount;
            order.Total = subtotal + shippingAmount - discount;
            order.DiscountAmount = discount;
            order.CouponCode = coupon?.Code;
            order.CouponName = coupon?.CampaignName;
            order.Status = OrderStatus.PendingShipment;
            order.CreatedAt = UtcNow;
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
                    CreatedAt = UtcNow
                });
            }
            await _context.SaveChangesAsync();
            return order;
    }

    public Task<bool> CancelAsync(int id) =>
        TransitionAsync(id, OrderStatus.Cancelled, true, OrderStatus.PendingShipment);

    public async Task<bool> RefundAsync(int id)
    {
        var hasActiveAfterSales = await _context.AfterSalesRequests.AnyAsync(request =>
            request.OrderId == id
            && request.Status != AfterSalesStatus.Completed
            && request.Status != AfterSalesStatus.Rejected);
        return !hasActiveAfterSales
            && await TransitionAsync(
                id,
                OrderStatus.Refunded,
                true,
                OrderStatus.ReceiptException,
                OrderStatus.Completed);
    }

    public async Task<bool> ShipAsync(int id, string? carrier, string? trackingNumber)
    {
        var order = await GetByIdAsync(id);
        if (order is null || order.Status != OrderStatus.PendingShipment)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(carrier)
            || string.IsNullOrWhiteSpace(trackingNumber)
            || carrier.Trim().Length > 50
            || trackingNumber.Trim().Length > 50)
        {
            return false;
        }

        order.Carrier = carrier.Trim();
        order.TrackingNumber = trackingNumber.Trim();
        order.ShippedAt = UtcNow;
        order.DeliveredAt = null;
        order.SignedAt = null;
        order.AutoConfirmAt = null;
        order.ReceiptConfirmedAutomatically = false;
        order.ReceiptExceptionReason = null;
        order.ReceiptExceptionReportedAt = null;
        order.ReceiptExceptionReportedBy = null;
        order.Status = OrderStatus.Shipped;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<OrderTrackingView?> GetTrackingAsync(int id)
    {
        var order = await GetByIdAsync(id);
        if (order is null || order.Status == OrderStatus.PendingShipment || order.ShippedAt is null)
        {
            return null;
        }

        var stateChangePaused = HasActiveAfterSales(order);
        if (!stateChangePaused)
        {
            await AdvanceDeliveryAsync(order);
            await TryAutoConfirmAsync(order);
        }

        var now = UtcNow;
        var shippedAt = order.ShippedAt!.Value;
        var deliveredAt = shippedAt.Add(DeliveryThreshold);
        var signed = order.Status == OrderStatus.Completed || order.SignedAt.HasValue;
        var receiptException = order.Status == OrderStatus.ReceiptException;
        var deliveredReached = order.DeliveredAt.HasValue || (!stateChangePaused && now >= deliveredAt);
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
                receiptException
                    ? L10n.Text("签收异常", "Receipt exception")
                    : signed
                        ? order.ReceiptConfirmedAutomatically
                            ? L10n.Text("自动确认收货", "Receipt auto-confirmed")
                            : L10n.Text("已签收", "Delivered & signed")
                        : L10n.Text("已送达，等待确认", "Delivered, awaiting confirmation"),
                receiptException
                    ? $"{L10n.Text("物流显示已送达，但买家反馈未收到商品。管理员备注：", "Tracking shows delivery, but the buyer reported not receiving the parcel. Admin note: ")}{order.ReceiptExceptionReason}"
                    : signed
                        ? order.ReceiptConfirmedAutomatically
                            ? $"{L10n.Text("包裹送达", "Delivered to")} {destination}，{L10n.Text("7 天确认期届满，系统已自动确认收货。", "the 7-day confirmation window expired and receipt was auto-confirmed.")}"
                            : $"{L10n.Text("买家已在", "Delivered to")} {destination} {L10n.Text("确认收货，订单完成。", "and receipt was confirmed.")}"
                        : $"{L10n.Text("包裹已送达", "Delivered to")} {destination}，{L10n.Text("等待买家确认收货，7 天后将自动确认。", "awaiting buyer confirmation. It will auto-confirm after 7 days.")}",
                signed ? order.SignedAt : (deliveredReached ? deliveredAt : null),
                signed || deliveredReached || receiptException,
                !signed && !receiptException && deliveredReached,
                !signed && !receiptException && !deliveredReached)
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
            AutoConfirmAt = order.AutoConfirmAt.HasValue
                ? DateTime.SpecifyKind(order.AutoConfirmAt.Value, DateTimeKind.Utc)
                : null,
            ReceiptExceptionReason = order.ReceiptExceptionReason,
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

        if (order.Status != OrderStatus.Delivered)
        {
            return false;
        }

        order.Status = OrderStatus.Completed;
        order.SignedAt = UtcNow;
        order.ReceiptConfirmedAutomatically = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReportReceiptExceptionAsync(int id, string reason, string reportedBy)
    {
        var normalizedReason = reason?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReason) || normalizedReason.Length > 500)
        {
            return false;
        }

        var order = await GetByIdAsync(id);
        if (order is null || order.Status != OrderStatus.Delivered)
        {
            return false;
        }

        order.Status = OrderStatus.ReceiptException;
        order.ReceiptExceptionReason = normalizedReason;
        order.ReceiptExceptionReportedAt = UtcNow;
        order.ReceiptExceptionReportedBy = string.IsNullOrWhiteSpace(reportedBy) ? "Admin" : reportedBy.Trim();
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

        order.UserDeletedAt = UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetActiveItemCountAsync(string userId)
    {
        await ProcessPendingOrderStatesAsync(userId);
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .Where(order =>
                order.UserId == userId
                && order.UserDeletedAt == null
                && order.Status != OrderStatus.Cancelled
                && order.Status != OrderStatus.Refunded
                && (order.Status != OrderStatus.Completed
                    || order.AfterSalesRequests.Any(request =>
                        request.Status != AfterSalesStatus.Completed
                        && request.Status != AfterSalesStatus.Rejected)))
            .ToListAsync();
        return orders.Sum(order => order.Items.Sum(item => item.Quantity));
    }

    public async Task<AfterSalesRequest> CreateAfterSalesAsync(int orderId, string userId, string type, string reason)
    {
        if (type is not (AfterSalesType.Return or AfterSalesType.Exchange or AfterSalesType.TransitCancellation))
        {
            throw new InvalidOperationException(L10n.Text("售后类型无效。", "Invalid after-sales type."));
        }

        var normalizedReason = reason.Trim();
        if (normalizedReason.Length is < 1 or > 1000)
        {
            throw new InvalidOperationException(L10n.Text(
                "售后原因不能为空且不能超过 1000 个字符。",
                "The after-sales reason must contain 1 to 1,000 characters."));
        }

        try
        {
            return await ExecuteInTransactionAsync(async () =>
            {
                var order = await GetByIdAsync(orderId);
                var eligibleStatus = type == AfterSalesType.TransitCancellation
                    ? order?.Status == OrderStatus.Shipped
                    : order?.Status is OrderStatus.Delivered or OrderStatus.Completed;
                if (order is null
                    || !string.Equals(order.UserId, userId, StringComparison.Ordinal)
                    || !eligibleStatus)
                {
                    throw new InvalidOperationException(L10n.Text(
                        "当前订单不能申请售后。",
                        "This order is not eligible for after-sales."));
                }

                var hasActiveRequest = await _context.AfterSalesRequests.AnyAsync(request =>
                    request.OrderId == orderId && request.Status != AfterSalesStatus.Rejected);
                if (hasActiveRequest)
                {
                    throw new InvalidOperationException(L10n.Text(
                        "该订单已有有效售后申请。",
                        "This order already has an active after-sales request."));
                }

                var request = new AfterSalesRequest
                {
                    OrderId = order.Id,
                    UserId = userId,
                    Type = type,
                    Reason = normalizedReason,
                    Status = AfterSalesStatus.Pending,
                    RequestedAt = UtcNow
                };
                _context.AfterSalesRequests.Add(request);
                await _context.SaveChangesAsync();
                return request;
            });
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintFailure(exception))
        {
            _context.ChangeTracker.Clear();
            throw new InvalidOperationException(L10n.Text(
                "该订单已有有效售后申请。",
                "This order already has an active after-sales request."), exception);
        }
    }

    public async Task<IReadOnlyList<AfterSalesRequest>> GetAfterSalesAllAsync() =>
        await _context.AfterSalesRequests
            .AsNoTracking()
            .Include(a => a.Order)
            .OrderByDescending(a => a.RequestedAt)
            .ToListAsync();

    public async Task<PagedResult<AfterSalesRequest>> GetAfterSalesPagedAsync(int page, int pageSize)
    {
        var query = _context.AfterSalesRequests
            .AsNoTracking()
            .Include(request => request.Order)
            .OrderByDescending(request => request.RequestedAt);
        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<AfterSalesRequest>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<AfterSalesUpdateViewModel>> GetAfterSalesUpdatesAsync(
        IReadOnlyCollection<int> ids)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<AfterSalesUpdateViewModel>();
        }

        var requests = await _context.AfterSalesRequests
            .AsNoTracking()
            .Where(request => ids.Contains(request.Id))
            .ToListAsync();
        return requests
            .Select(AfterSalesUpdateViewModel.FromRequest)
            .ToList();
    }

    public Task<int> CountOpenAfterSalesAsync() =>
        _context.AfterSalesRequests.CountAsync(request =>
            request.Status != AfterSalesStatus.Completed
            && request.Status != AfterSalesStatus.Rejected);

    public async Task<bool> ReviewAfterSalesAsync(int id, bool approved, string? note)
    {
        if (note?.Trim().Length > 500)
        {
            return false;
        }

        var request = await _context.AfterSalesRequests.FirstOrDefaultAsync(a => a.Id == id);
        if (request is null
            || request.Status != AfterSalesStatus.Pending
            || request.Type == AfterSalesType.TransitCancellation)
        {
            return false;
        }

        request.Status = approved ? AfterSalesStatus.Approved : AfterSalesStatus.Rejected;
        request.AdminNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        request.ReviewedAt = UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResolveTransitCancellationAsync(int id, bool intercepted, string? note)
    {
        if (note?.Trim().Length > 500)
        {
            return false;
        }

        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        try
        {
            var request = await _context.AfterSalesRequests
                .Include(a => a.Order)
                .ThenInclude(o => o!.Items)
                .FirstOrDefaultAsync(a =>
                    a.Id == id
                    && a.Type == AfterSalesType.TransitCancellation
                    && a.Status == AfterSalesStatus.Pending);
            if (request?.Order is null)
            {
                return false;
            }

            request.AdminNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            request.ReviewedAt = DateTime.UtcNow;
            if (!intercepted)
            {
                request.Status = AfterSalesStatus.Rejected;
                await _context.SaveChangesAsync();
                if (transaction is not null)
                {
                    await transaction.CommitAsync();
                }
                return true;
            }

            if (request.Order.Status != OrderStatus.Shipped)
            {
                return false;
            }

            await RestoreStockAsync(request.Order);
            await ReleaseCouponAsync(request.Order);
            request.Order.Status = OrderStatus.Refunded;
            request.Status = AfterSalesStatus.Completed;
            request.CompletedAt = DateTime.UtcNow;
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

    public async Task<bool> SubmitReturnShipmentAsync(
        int id,
        string userId,
        string? carrier,
        string? trackingNumber)
    {
        var normalizedCarrier = carrier?.Trim();
        var normalizedTracking = trackingNumber?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedCarrier)
            || string.IsNullOrWhiteSpace(normalizedTracking)
            || normalizedCarrier.Length > 50
            || normalizedTracking.Length > 50)
        {
            return false;
        }

        var request = await _context.AfterSalesRequests.FirstOrDefaultAsync(a => a.Id == id);
        if (request is null
            || !string.Equals(request.UserId, userId, StringComparison.Ordinal)
            || request.Status != AfterSalesStatus.Approved
            || request.Type is not (AfterSalesType.Return or AfterSalesType.Exchange))
        {
            return false;
        }

        request.ReturnCarrier = normalizedCarrier;
        request.ReturnTrackingNumber = normalizedTracking;
        request.ReturnShippedAt = DateTime.UtcNow;
        request.Status = AfterSalesStatus.Returning;
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception exception) when (IsConcurrencyFailure(exception))
        {
            _context.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task<OrderTrackingView?> GetReturnTrackingAsync(int id, string? userId = null)
    {
        var request = await _context.AfterSalesRequests
            .AsNoTracking()
            .Include(a => a.Order)
            .FirstOrDefaultAsync(a =>
                a.Id == id
                && (a.Type == AfterSalesType.Return || a.Type == AfterSalesType.Exchange)
                && a.ReturnShippedAt.HasValue
                && !string.IsNullOrEmpty(a.ReturnCarrier)
                && !string.IsNullOrEmpty(a.ReturnTrackingNumber));
        if (request?.Order is null
            || (userId is not null && !string.Equals(request.UserId, userId, StringComparison.Ordinal)))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var shippedAt = request.ReturnShippedAt!.Value;
        var estimatedWarehouseArrivalAt = shippedAt.Add(DeliveryThreshold);
        var warehouseReceived = request.ReceivedAt.HasValue
            || request.Status is AfterSalesStatus.Received or AfterSalesStatus.Completed;
        var origin = string.IsNullOrWhiteSpace(request.Order.Address)
            ? L10n.Text("买家收货地址", "Buyer delivery address")
            : request.Order.Address;
        var destination = !string.IsNullOrWhiteSpace(request.Order.ShippingOrigin)
            ? L10n.Text(request.Order.ShippingOrigin, request.Order.ShippingOriginEn)
            : L10n.Text("原发货地 / 仓库", "Original dispatch location / warehouse");

        var nodes = new List<OrderTrackingNode>
        {
            Node(
                1,
                L10n.Text("买家已寄回", "Return handed to carrier"),
                $"{L10n.Text("退货包裹已从", "The return parcel departed")} {origin} {L10n.Text("交给", "with")} {L10n.Carrier(request.ReturnCarrier)} ({request.ReturnTrackingNumber})",
                shippedAt,
                true,
                false),
            Node(
                2,
                L10n.Text("退货包裹已离开买家所在地区", "Return parcel left the buyer's region"),
                $"{L10n.Text("退货包裹正在发往", "The return parcel is travelling to")} {destination}",
                shippedAt.Add(TrackingStep),
                now >= shippedAt.Add(TrackingStep),
                now >= shippedAt.Add(TrackingStep) && now < shippedAt.Add(TrackingStep * 2),
                now < shippedAt.Add(TrackingStep)),
            Node(
                3,
                L10n.Text("退货包裹已到达仓库城市", "Return parcel reached the warehouse city"),
                $"{L10n.Text("承运商正在将退货包裹送往", "The carrier is moving the return parcel to")} {destination}",
                shippedAt.Add(TrackingStep * 2),
                now >= shippedAt.Add(TrackingStep * 2),
                now >= shippedAt.Add(TrackingStep * 2) && !warehouseReceived,
                now < shippedAt.Add(TrackingStep * 2)),
            Node(
                4,
                warehouseReceived
                    ? L10n.Text("仓库已收货", "Warehouse received")
                    : L10n.Text("到达发货地，等待验收", "Arrived, awaiting inspection"),
                warehouseReceived
                    ? $"{L10n.Text("退回商品已由", "The returned item was received by")} {destination} {L10n.Text("签收入库。", "and taken into inventory.")}"
                    : $"{L10n.Text("物流信息显示退货包裹已到达", "Tracking indicates the return parcel reached")} {destination}，{L10n.Text("等待管理员核对并标记已收货。", "awaiting admin verification and receipt.")}",
                warehouseReceived ? request.ReceivedAt ?? estimatedWarehouseArrivalAt : estimatedWarehouseArrivalAt,
                warehouseReceived || now >= estimatedWarehouseArrivalAt,
                !warehouseReceived && now >= estimatedWarehouseArrivalAt,
                !warehouseReceived && now < estimatedWarehouseArrivalAt)
        };

        return new OrderTrackingView
        {
            OrderId = request.OrderId,
            OrderNo = request.Order.OrderNo,
            Carrier = L10n.Carrier(request.ReturnCarrier),
            TrackingNumber = request.ReturnTrackingNumber!,
            OfficialTrackingUrl = $"https://t.17track.net/en#nums={Uri.EscapeDataString(request.ReturnTrackingNumber!)}",
            OriginLocation = origin,
            DestinationCity = destination,
            RecipientName = request.Order.UserName,
            Status = request.Status,
            ShippedAt = shippedAt,
            CompletedAt = request.CompletedAt,
            Nodes = nodes
        };
    }

    public async Task<bool> MarkAfterSalesReceivedAsync(int id)
    {
        var request = await _context.AfterSalesRequests.FirstOrDefaultAsync(a => a.Id == id);
        if (request is null
            || request.Status != AfterSalesStatus.Returning
            || string.IsNullOrWhiteSpace(request.ReturnCarrier)
            || string.IsNullOrWhiteSpace(request.ReturnTrackingNumber)
            || !request.ReturnShippedAt.HasValue
            || request.Type == AfterSalesType.TransitCancellation)
        {
            return false;
        }

        request.Status = AfterSalesStatus.Received;
        request.ReceivedAt = UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CompleteAfterSalesAsync(int id)
    {
        try
        {
            return await ExecuteInTransactionAsync(async () =>
            {
                var request = await _context.AfterSalesRequests
                    .Include(a => a.Order)
                    .ThenInclude(o => o!.Items)
                    .FirstOrDefaultAsync(a => a.Id == id);
                if (request is null
                    || request.Status != AfterSalesStatus.Received
                    || request.Order is null
                    || request.Type == AfterSalesType.TransitCancellation)
                {
                    return false;
                }

                if (request.Type == AfterSalesType.Return)
                {
                    await RestoreStockAsync(request.Order);
                    request.Order.Status = OrderStatus.Refunded;
                }
                else if (request.Type == AfterSalesType.Exchange)
                {
                    await RestoreStockAsync(request.Order);
                    var replacement = BuildReplacementOrder(request.Order);
                    if (!await ReserveStockAsync(replacement))
                    {
                        _context.ChangeTracker.Clear();
                        return false;
                    }
                    _context.Orders.Add(replacement);
                    request.Order.Status = OrderStatus.Completed;
                }

                request.Status = AfterSalesStatus.Completed;
                request.CompletedAt = UtcNow;
                await _context.SaveChangesAsync();
                return true;
            });
        }
        catch (Exception exception) when (IsConcurrencyFailure(exception)
            || IsUniqueConstraintFailure(exception)
            || exception is OverflowException)
        {
            _context.ChangeTracker.Clear();
            return false;
        }
    }

    public Task<int> CountAsync() => _context.Orders.CountAsync();

    public Task<int> ProcessPendingOrderStatesAsync() => ProcessPendingOrderStatesAsync(null);

    private async Task<int> ProcessPendingOrderStatesAsync(string? userId)
    {
        var query = _context.Orders.Where(order =>
            (order.Status == OrderStatus.Shipped
                && !order.AfterSalesRequests.Any(request =>
                    request.Type == AfterSalesType.TransitCancellation
                    && request.Status == AfterSalesStatus.Pending))
            || (order.Status == OrderStatus.Delivered
                && !order.AfterSalesRequests.Any(request =>
                    request.Status != AfterSalesStatus.Completed
                    && request.Status != AfterSalesStatus.Rejected)));
        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(order => order.UserId == userId);
        }

        var orders = await query.ToListAsync();
        var changed = 0;
        foreach (var order in orders)
        {
            if (order.Status == OrderStatus.Shipped)
            {
                await AdvanceDeliveryAsync(order);
                changed++;
            }

            if (order.Status == OrderStatus.Delivered)
            {
                await TryAutoConfirmAsync(order);
                changed++;
            }
        }

        return changed;
    }

    private async Task AdvanceDeliveryAsync(Order order)
    {
        if (order.Status != OrderStatus.Shipped || order.ShippedAt is null)
        {
            return;
        }

        if (UtcNow < order.ShippedAt.Value.Add(DeliveryThreshold))
        {
            return;
        }

        order.Status = OrderStatus.Delivered;
        order.DeliveredAt = order.ShippedAt.Value.Add(DeliveryThreshold);
        order.AutoConfirmAt = order.DeliveredAt.Value.Add(AutoConfirmationWindow);
        await _context.SaveChangesAsync();
    }

    private async Task TryAutoConfirmAsync(Order order)
    {
        if (order.Status != OrderStatus.Delivered)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (!order.AutoConfirmAt.HasValue)
        {
            order.AutoConfirmAt = (order.DeliveredAt ?? now).Add(AutoConfirmationWindow);
            await _context.SaveChangesAsync();
        }

        if (order.AutoConfirmAt.Value > now)
        {
            return;
        }

        order.Status = OrderStatus.Completed;
        order.SignedAt = order.AutoConfirmAt.Value;
        order.ReceiptConfirmedAutomatically = true;
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

    private void ApplyShippingSnapshot(Order order, IReadOnlyList<Product> products)
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
        order.ExpectedDispatchAt = UtcNow.AddHours(dispatchHours);
    }

    private static decimal CalculateShippingAmount(IReadOnlyList<Product> products, bool freeShipping) =>
        freeShipping
            ? 0
            : products
                .Select(product => product.ShippingFeeAmount)
                .Where(amount => amount > 0)
                .Sum();

    private Order BuildReplacementOrder(Order source) => new()
    {
        OrderNo = _orderNumberGenerator.Generate(),
        UserId = source.UserId,
        UserName = source.UserName,
        Phone = source.Phone,
        Address = source.Address,
        Total = 0,
        DiscountAmount = 0,
        ShippingAmount = 0,
        PaymentMethod = source.PaymentMethod,
        OrderNotes = source.OrderNotes,
        Status = OrderStatus.PendingShipment,
        ShippingOrigin = source.ShippingOrigin,
        ShippingOriginEn = source.ShippingOriginEn,
        ShippingFeeText = source.ShippingFeeText,
        ShippingFeeTextEn = source.ShippingFeeTextEn,
        DispatchTimeText = source.DispatchTimeText,
        DispatchTimeTextEn = source.DispatchTimeTextEn,
        ExpectedDispatchAt = UtcNow.AddHours(48),
        ReplacementForOrderId = source.Id,
        CreatedAt = UtcNow,
        Items = source.Items.Select(item => new OrderItem
        {
            ProductId = item.ProductId,
            SkuId = item.SkuId,
            ProductName = item.ProductName,
            ProductNameEn = item.ProductNameEn,
            ImageUrl = item.ImageUrl,
            Color = item.Color,
            ColorEn = item.ColorEn,
            Spec = item.Spec,
            SpecEn = item.SpecEn,
            Price = item.Price,
            Quantity = item.Quantity
        }).ToList()
    };

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

    private async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
    {
        if (!_context.Database.IsRelational())
        {
            return await operation();
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var result = await operation();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                _context.ChangeTracker.Clear();
                throw;
            }
        });
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
        try
        {
            return await ExecuteInTransactionAsync(async () =>
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
                return true;
            });
        }
        catch (Exception exception) when (IsConcurrencyFailure(exception) || exception is OverflowException)
        {
            _context.ChangeTracker.Clear();
            return false;
        }
    }

    private async Task RestoreStockAsync(Order order)
    {
        if (order.InventoryRestoredAt.HasValue)
        {
            return;
        }

        order.InventoryRestoredAt = UtcNow;
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

    private async Task<bool> ReserveStockAsync(Order order)
    {
        var productQuantities = order.Items
            .GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(item => (long)item.Quantity));
        var skuQuantities = order.Items
            .Where(item => item.SkuId.HasValue)
            .GroupBy(item => item.SkuId!.Value)
            .ToDictionary(group => group.Key, group => group.Sum(item => (long)item.Quantity));

        var productIds = productQuantities.Keys.ToList();
        var products = await _context.Products.Where(product => productIds.Contains(product.Id)).ToListAsync();
        if (products.Count != productIds.Count || products.Any(product =>
                !product.IsActive || productQuantities[product.Id] > product.Stock))
        {
            return false;
        }

        var skuIds = skuQuantities.Keys.ToList();
        var skus = await _context.ProductSkus.Where(sku => skuIds.Contains(sku.Id)).ToListAsync();
        if (skus.Count != skuIds.Count || skus.Any(sku =>
                !sku.IsActive || skuQuantities[sku.Id] > sku.Stock))
        {
            return false;
        }

        foreach (var product in products)
        {
            product.Stock = checked(product.Stock - (int)productQuantities[product.Id]);
        }
        foreach (var sku in skus)
        {
            sku.Stock = checked(sku.Stock - (int)skuQuantities[sku.Id]);
        }
        return true;
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

        redemption.ReleasedAt = UtcNow;
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

    private static bool HasActiveAfterSales(Order order) =>
        order.AfterSalesRequests.Any(request =>
            (order.Status == OrderStatus.Shipped
                && request.Type == AfterSalesType.TransitCancellation
                && request.Status == AfterSalesStatus.Pending)
            || (order.Status == OrderStatus.Delivered
                && request.Status != AfterSalesStatus.Completed
                && request.Status != AfterSalesStatus.Rejected));

    private static bool IsUniqueConstraintFailure(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } })
            {
                return true;
            }
        }
        return false;
    }
}
