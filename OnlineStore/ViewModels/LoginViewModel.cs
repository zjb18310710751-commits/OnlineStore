using System.ComponentModel.DataAnnotations;

namespace OnlineStore.ViewModels;

/// <summary>登录表单视图模型。</summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "AccountRequired")]
    [Display(Name = "邮箱 / 手机号")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "PasswordRequired")]
    [DataType(DataType.Password)]
    [Display(Name = "密码")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "记住我")]
    public bool RememberMe { get; set; }

    [Display(Name = "记住账号密码")]
    public bool RememberCredentials { get; set; }
}
