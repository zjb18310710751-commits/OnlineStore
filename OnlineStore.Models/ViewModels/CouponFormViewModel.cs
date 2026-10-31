using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnlineStore.Models;

namespace OnlineStore.ViewModels;

public class CouponFormViewModel
{
    public int Id { get; set; }

    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "CouponNameRequired")]
    [StringLength(100)]
    public string CampaignName { get; set; } = string.Empty;

    public CouponDiscountType DiscountType { get; set; } = CouponDiscountType.Percent;

    [Range(0, 1000000, ErrorMessage = "CouponValueInvalid")]
    public decimal DiscountValue { get; set; }

    [Range(0, 1000000, ErrorMessage = "MaxDiscountAmountInvalid")]
    public decimal? MaxDiscountAmount { get; set; }

    [Range(0, 1000000, ErrorMessage = "MinOrderAmountInvalid")]
    public decimal? MinOrderAmount { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(1, int.MaxValue, ErrorMessage = "UsageLimitInvalid")]
    public int? UsageLimit { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "PerUserLimitInvalid")]
    public int? PerUserLimit { get; set; } = 1;

    public bool FirstOrderOnly { get; set; }

    public bool ExcludeDiscountedItems { get; set; }

    public CouponScope Scope { get; set; } = CouponScope.AllProducts;

    public List<int> CategoryIds { get; set; } = new();

    public List<int> ProductIds { get; set; } = new();

    public IReadOnlyList<SelectListItem> CategoryOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> ProductOptions { get; set; } = Array.Empty<SelectListItem>();
}

public class CouponBatchViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "CouponBatchFailed")]
    public int CouponId { get; set; }

    [Range(1, 50, ErrorMessage = "CouponBatchQuantityInvalid")]
    public int Quantity { get; set; } = 5;
}
