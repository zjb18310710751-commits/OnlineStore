using System.ComponentModel.DataAnnotations;
using OnlineStore.Models;

namespace OnlineStore.ViewModels;

/// <summary>结算页视图模型：购物车明细 + 收货信息。</summary>
public class CheckoutViewModel
{
    public List<CartItem> Items { get; set; } = new();

    public decimal Total { get; set; }

    /// <summary>结算页当前选中的地址簿地址；为空表示用户尚未选择或使用旧地址。</summary>
    public int? AddressId { get; set; }

    /// <summary>用户是否已经保存过结构化收货地址（用于决定是否显示“保存为默认地址”）。</summary>
    public bool HasSavedAddresses { get; set; }

    /// <summary>新用户结算时是否把当前填写的地址保存为默认地址。</summary>
    public bool SaveAsDefaultAddress { get; set; }

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

    [Display(Name = "地址第二行（可选）")]
    public string? AddressLine2 { get; set; }

    [Display(Name = "城市")]
    public string? City { get; set; }

    [Display(Name = "邮编")]
    public string? PostalCode { get; set; }

    [Display(Name = "国家")]
    public string? Country { get; set; }

    [Display(Name = "支付方式")]
    public string PaymentMethod { get; set; } = "cash_on_delivery";

    [Display(Name = "订单备注（可选）")]
    public string? OrderNotes { get; set; }
}
