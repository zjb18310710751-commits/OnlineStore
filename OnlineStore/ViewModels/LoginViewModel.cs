using System.ComponentModel.DataAnnotations;

namespace OnlineStore.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "AccountRequired")]
    [Display(Name = "邮箱")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "PasswordRequired")]
    [DataType(DataType.Password)]
    [Display(Name = "密码")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "记住我")]
    public bool RememberMe { get; set; }

    [Display(Name = "记住账号")]
    public bool RememberCredentials { get; set; }
}
