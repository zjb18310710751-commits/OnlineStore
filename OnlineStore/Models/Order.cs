using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Models;

public class Order
{
    public int Id { get; set; }

    [StringLength(30)]
    [Display(Name = "订单号")]
    public string OrderNo { get; set; } = string.Empty;

    public string? UserId { get; set; }

    [StringLength(50)]
    [Display(Name = "收货人")]
    public string UserName { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "联系电话")]
    public string? Phone { get; set; }

    [StringLength(500)]
    [Display(Name = "收货地址")]
    public string? Address { get; set; }

    [Display(Name = "订单金额")]
    public decimal Total { get; set; }

    [StringLength(50)]
    public string? CouponCode { get; set; }

    [StringLength(100)]
    public string? CouponName { get; set; }

    public decimal DiscountAmount { get; set; }

    [StringLength(20)]
    [Display(Name = "订单状态")]
    public string Status { get; set; } = "待发货";

    [StringLength(50)]
    [Display(Name = "承运商")]
    public string? Carrier { get; set; }

    [StringLength(50)]
    [Display(Name = "运单号")]
    public string? TrackingNumber { get; set; }

    [StringLength(200)]
    public string? ShippingOrigin { get; set; }

    [StringLength(200)]
    public string? ShippingOriginEn { get; set; }

    [StringLength(100)]
    public string? ShippingFeeText { get; set; }

    [StringLength(100)]
    public string? ShippingFeeTextEn { get; set; }

    [StringLength(100)]
    public string? DispatchTimeText { get; set; }

    [StringLength(100)]
    public string? DispatchTimeTextEn { get; set; }

    public DateTime? ExpectedDispatchAt { get; set; }

    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? SignedAt { get; set; }

    public DateTime? UserDeletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    public ICollection<AfterSalesRequest> AfterSalesRequests { get; set; } = new List<AfterSalesRequest>();
}
