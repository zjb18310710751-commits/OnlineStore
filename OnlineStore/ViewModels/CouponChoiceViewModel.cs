using OnlineStore.Models;

namespace OnlineStore.ViewModels;

public class CouponChoiceViewModel
{
    public string Code { get; set; } = string.Empty;

    public string? CampaignName { get; set; }

    public CouponDiscountType DiscountType { get; set; }

    public decimal DiscountValue { get; set; }

    public decimal CalculatedDiscount { get; set; }

    public bool FreeShipping { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsBest { get; set; }

    public bool IsSelected { get; set; }
}
