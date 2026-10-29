using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Models;

public class CouponRedemption
{
    public int Id { get; set; }

    public int CouponId { get; set; }

    public Coupon? Coupon { get; set; }

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    public int OrderId { get; set; }

    public Order? Order { get; set; }

    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    public decimal DiscountAmount { get; set; }

    public bool ShippingWaived { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReleasedAt { get; set; }
}
