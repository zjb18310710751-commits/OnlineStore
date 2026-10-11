using System.ComponentModel.DataAnnotations;
using OnlineStore.Models;

namespace OnlineStore.ViewModels;

/// <summary>结算页视图模型：购物车明细 + 收货信息。</summary>
public class CheckoutViewModel
{
    public List<CartItem> Items { get; set; } = new();

    public decimal Total { get; set; }

    [Required(ErrorMessage = "请输入收货人姓名")]
    [StringLength(50)]
    [Display(Name = "收货人")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入联系电话")]
    [StringLength(20)]
    [Display(Name = "联系电话")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入收货地址")]
    [StringLength(500)]
    [Display(Name = "收货地址")]
    public string Address { get; set; } = string.Empty;
}
