using System.ComponentModel.DataAnnotations;

namespace OnlineStore.ViewModels;

/// <summary>个人中心：查看/修改用户资料（邮箱只读）。</summary>
public class ProfileViewModel
{
    [Required(ErrorMessage = "请输入名")]
    [StringLength(100)]
    [Display(Name = "名")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入姓")]
    [StringLength(100)]
    [Display(Name = "姓")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "邮箱")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入电话号码")]
    [RegularExpression(@"^[0-9+\-\s()]{6,20}$", ErrorMessage = "电话号码格式不正确")]
    [Display(Name = "电话号码")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入地址")]
    [StringLength(500)]
    [Display(Name = "地址")]
    public string Address { get; set; } = string.Empty;
}
