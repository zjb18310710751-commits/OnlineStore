using System.ComponentModel.DataAnnotations;

namespace OnlineStore.ViewModels;

/// <summary>注册表单视图模型。</summary>
public class RegisterViewModel
{
    [Required(ErrorMessage = "请输入名")]
    [StringLength(100)]
    [Display(Name = "名")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入姓")]
    [StringLength(100)]
    [Display(Name = "姓")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入邮箱")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    [Display(Name = "邮箱")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入地址")]
    [StringLength(500)]
    [Display(Name = "地址")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入电话号码")]
    [RegularExpression(@"^[0-9+\-\s()]{6,20}$", ErrorMessage = "电话号码格式不正确")]
    [Display(Name = "电话号码")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入密码")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密码至少 6 位")]
    [DataType(DataType.Password)]
    [Display(Name = "密码")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "确认密码")]
    [Compare(nameof(Password), ErrorMessage = "两次输入的密码不一致")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
