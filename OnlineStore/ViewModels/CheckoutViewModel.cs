using System.ComponentModel.DataAnnotations;
using OnlineStore.Models;

namespace OnlineStore.ViewModels;

public class CheckoutViewModel
{
    public List<CartItem> Items { get; set; } = new();

    public decimal Total { get; set; }

    public int? AddressId { get; set; }

    public bool HasSavedAddresses { get; set; }

    public bool SaveAsDefaultAddress { get; set; }

    [StringLength(50)]
    public string? CouponCode { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal ShippingFee { get; set; }

    public bool FreeShippingCoupon { get; set; }

    public List<CouponChoiceViewModel> AvailableCoupons { get; set; } = new();

    public decimal PayableTotal => Math.Max(0, Total + ShippingFee - DiscountAmount);

    [Display(Name = "收货人")]
    public string? UserName { get; set; }

    [Display(Name = "联系电话")]
    public string? Phone { get; set; }

    [Display(Name = "收货地址")]
    public string? Address { get; set; }

    [Display(Name = "地址第二行（可选）")]
    public string? AddressLine2 { get; set; }

    [Display(Name = "城市")]
    public string? City { get; set; }

    [Display(Name = "邮编")]
    public string? PostalCode { get; set; }

    [Display(Name = "国家")]
    public string? Country { get; set; }

    [Required]
    [Display(Name = "支付方式")]
    public string PaymentMethod { get; set; } = "cash_on_delivery";

    [StringLength(500)]
    [Display(Name = "订单备注（可选）")]
    public string? OrderNotes { get; set; }
}
