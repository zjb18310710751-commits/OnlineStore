using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Models;

/// <summary>订单：用户从购物车提交后生成，含收货信息与订单明细。</summary>
public class Order
{
    public int Id { get; set; }

    [StringLength(30)]
    [Display(Name = "订单号")]
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>下单用户 Id。</summary>
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

    /// <summary>使用的优惠码（如有）。</summary>
    [StringLength(50)]
    public string? CouponCode { get; set; }

    /// <summary>优惠金额（无优惠为 0）。</summary>
    public decimal DiscountAmount { get; set; }

    [StringLength(20)]
    [Display(Name = "订单状态")]
    public string Status { get; set; } = "待发货";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
