using System.ComponentModel.DataAnnotations;

namespace OnlineStore.ViewModels;

/// <summary>发送邮箱验证码请求模型。</summary>
public class SendCodeViewModel
{
    [Required(ErrorMessage = "请输入邮箱")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string Email { get; set; } = string.Empty;

    /// <summary>用途：register（注册，要求邮箱未注册）或 reset（找回密码，要求邮箱已注册）。</summary>
    public string Purpose { get; set; } = "register";
}
