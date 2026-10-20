using System.ComponentModel.DataAnnotations;

namespace OnlineStore.ViewModels;

/// <summary>忘记密码：用邮箱验证码重置密码的表单。</summary>
public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "请输入邮箱")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    [Display(Name = "邮箱")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入验证码")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "验证码为 6 位数字")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "验证码为 6 位数字")]
    [Display(Name = "验证码")]
    public string VerificationCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入新密码")]
    [StringLength(100, ErrorMessage = "密码最长 100 位")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9]).{6,}$", ErrorMessage = "密码至少 6 位，须包含大写字母、小写字母、数字和特殊字符")]
    [DataType(DataType.Password)]
    [Display(Name = "新密码")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "确认密码")]
    [Compare(nameof(NewPassword), ErrorMessage = "两次输入的密码不一致")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
