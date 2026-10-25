using System.ComponentModel.DataAnnotations;
using OnlineStore.Validation;

namespace OnlineStore.ViewModels;

public class RegisterViewModel
{

    public string RegisterBy { get; set; } = "email";

    [Display(Name = "邮箱")]
    [StringLength(256)]
    public string? Email { get; set; }

    [Display(Name = "手机号")]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "VerificationCodeRequired")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "VerificationCodeFormat")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "VerificationCodeFormat")]
    [Display(Name = "验证码")]
    public string VerificationCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "FirstNameRequired")]
    [StringLength(100)]
    [Display(Name = "名")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "LastNameRequired")]
    [StringLength(100)]
    [Display(Name = "姓")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "地址")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "AddressRequired"), StringLength(200)]
    public string? AddressLine1 { get; set; }

    [StringLength(200)]
    public string? AddressLine2 { get; set; }

    [Required(ErrorMessage = "CityRequired"), StringLength(100)]
    public string? City { get; set; }

    [Required(ErrorMessage = "PostalCodeRequired"), StringLength(100)]
    public string? PostalCode { get; set; }

    [Required(ErrorMessage = "CountryRequired"), StringLength(100)]
    public string? Country { get; set; }

    [Required(ErrorMessage = "PasswordRequired")]
    [StringLength(100, ErrorMessage = "PasswordTooLong")]
    [RegularExpression(PasswordPolicy.Pattern, ErrorMessage = "PasswordPolicyError")]
    [DataType(DataType.Password)]
    [Display(Name = "密码")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "确认密码")]
    [Compare(nameof(Password), ErrorMessage = "PasswordsDoNotMatch")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
