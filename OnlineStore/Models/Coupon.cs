using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Models;

/// <summary>优惠券折扣类型。</summary>
public enum CouponDiscountType
{
    /// <summary>按订单金额百分比折扣。</summary>
    Percent = 0,

    /// <summary>固定金额抵扣。</summary>
    Fixed = 1
}

/// <summary>优惠券：后台创建，用户下单时输入优惠码抵扣订单金额。</summary>
public class Coupon
{
    public int Id { get; set; }

    [Required(ErrorMessage = "CouponCodeRequired")]
    [StringLength(50)]
    [Display(Name = "优惠码")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "优惠类型")]
    public CouponDiscountType DiscountType { get; set; } = CouponDiscountType.Percent;

    [Range(0.01, 1000000, ErrorMessage = "CouponValueInvalid")]
    [Display(Name = "优惠值")]
    public decimal DiscountValue { get; set; }

    [Range(0, 1000000, ErrorMessage = "MinOrderAmountInvalid")]
    [Display(Name = "最低订单金额")]
    public decimal? MinOrderAmount { get; set; }

    [Display(Name = "开始日期")]
    public DateTime StartDate { get; set; }

    [Display(Name = "结束日期")]
    public DateTime EndDate { get; set; }

    [Display(Name = "启用")]
    public bool IsActive { get; set; } = true;

    [Range(1, int.MaxValue, ErrorMessage = "UsageLimitInvalid")]
    [Display(Name = "使用上限")]
    public int? UsageLimit { get; set; }

    [Display(Name = "已用次数")]
    public int UsedCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
