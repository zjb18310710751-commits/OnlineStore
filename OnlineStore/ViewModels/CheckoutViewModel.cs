using System.ComponentModel.DataAnnotations;
using OnlineStore.Models;

namespace OnlineStore.ViewModels;

/// <summary>结算页视图模型：购物车明细 + 收货信息。</summary>
public class CheckoutViewModel
{
    public List<CartItem> Items { get; set; } = new();

    public decimal Total { get; set; }

    [StringLength(50)]
    public string? CouponCode { get; set; }

    public decimal DiscountAmount { get; set; }

    /// <summary>实付金额 = 商品总额 - 优惠。</summary>
    public decimal PayableTotal => Total - DiscountAmount;

    [Display(Name = "收货人")]
    public string? UserName { get; set; }

    [Display(Name = "联系电话")]
    public string? Phone { get; set; }

    [Display(Name = "收货地址")]
    public string? Address { get; set; }
}
