using System.ComponentModel.DataAnnotations;
using OnlineStore.Validation;

namespace OnlineStore.ViewModels;

/// <summary>忘记密码：用邮箱验证码重置密码的表单。</summary>
public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "EmailRequired")]
    [EmailAddress(ErrorMessage = "InvalidEmail")]
    [Display(Name = "邮箱")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "VerificationCodeRequired")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "VerificationCodeFormat")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "VerificationCodeFormat")]
    [Display(Name = "验证码")]
    public string VerificationCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "NewPasswordRequired")]
    [StringLength(100, ErrorMessage = "PasswordTooLong")]
    [RegularExpression(PasswordPolicy.Pattern, ErrorMessage = "PasswordPolicyError")]
    [DataType(DataType.Password)]
    [Display(Name = "新密码")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "确认密码")]
    [Compare(nameof(NewPassword), ErrorMessage = "PasswordsDoNotMatch")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
