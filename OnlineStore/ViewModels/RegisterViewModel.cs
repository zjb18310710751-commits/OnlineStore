using System.ComponentModel.DataAnnotations;

namespace OnlineStore.ViewModels;

/// <summary>注册表单视图模型（邮箱注册与手机号注册共用，通过 <see cref="RegisterBy"/> 区分）。</summary>
public class RegisterViewModel
{
    /// <summary>注册方式：email（邮箱注册）或 phone（手机号注册）。</summary>
    public string RegisterBy { get; set; } = "email";

    [Display(Name = "邮箱")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "手机号")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入验证码")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "验证码为 6 位数字")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "验证码为 6 位数字")]
    [Display(Name = "验证码")]
    public string VerificationCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入名")]
    [StringLength(100)]
    [Display(Name = "名")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入姓")]
    [StringLength(100)]
    [Display(Name = "姓")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入地址")]
    [StringLength(500)]
    [Display(Name = "地址")]
    public string Address { get; set; } = string.Empty;

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
