using System.ComponentModel.DataAnnotations;

namespace OnlineStore.Models;

public enum CouponDiscountType
{
    Percent = 0,
    Fixed = 1,
    FreeShipping = 2
}

public enum CouponScope
{
    AllProducts = 0,
    Categories = 1,
    Products = 2
}

public class Coupon
{
    public int Id { get; set; }

    [Required(ErrorMessage = "CouponCodeRequired")]
    [StringLength(50)]
    [Display(Name = "优惠码")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "优惠类型")]
    public CouponDiscountType DiscountType { get; set; } = CouponDiscountType.Percent;

    [Range(0, 1000000, ErrorMessage = "CouponValueInvalid")]
    [Display(Name = "优惠值")]
    public decimal DiscountValue { get; set; }

    [Range(0, 1000000, ErrorMessage = "MaxDiscountAmountInvalid")]
    [Display(Name = "最高优惠金额")]
    public decimal? MaxDiscountAmount { get; set; }

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

    [Range(1, int.MaxValue, ErrorMessage = "PerUserLimitInvalid")]
    [Display(Name = "每用户限用次数")]
    public int? PerUserLimit { get; set; } = 1;

    [Display(Name = "仅限首单")]
    public bool FirstOrderOnly { get; set; }

    [Display(Name = "排除促销商品")]
    public bool ExcludeDiscountedItems { get; set; }

    [Display(Name = "适用商品范围")]
    public CouponScope Scope { get; set; } = CouponScope.AllProducts;

    [StringLength(100)]
    [Display(Name = "活动名称/备注")]
    public string? CampaignName { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Category> Categories { get; set; } = new List<Category>();

    public ICollection<Product> Products { get; set; } = new List<Product>();

    public ICollection<CouponRedemption> Redemptions { get; set; } = new List<CouponRedemption>();
}
