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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
