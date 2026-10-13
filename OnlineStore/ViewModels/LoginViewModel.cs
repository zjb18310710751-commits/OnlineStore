using System.ComponentModel.DataAnnotations;

namespace OnlineStore.ViewModels;

/// <summary>登录表单视图模型。</summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "请输入邮箱或手机号")]
    [Display(Name = "邮箱 / 手机号")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入密码")]
    [DataType(DataType.Password)]
    [Display(Name = "密码")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "记住我")]
    public bool RememberMe { get; set; }

    [Display(Name = "记住账号密码")]
    public bool RememberCredentials { get; set; }
}
