using System.ComponentModel.DataAnnotations;

namespace OnlineStore.ViewModels;

/// <summary>发送验证码请求模型：目标地址（邮箱或手机号）+ 渠道 + 用途。</summary>
public class SendCodeViewModel
{
    [Required(ErrorMessage = "请输入邮箱或手机号")]
    public string Target { get; set; } = string.Empty;

    /// <summary>渠道：email（邮箱）或 sms（手机号，演示模式）。</summary>
    public string Channel { get; set; } = "email";

    /// <summary>用途：register（注册，要求目标地址未注册）或 reset（找回密码，要求邮箱已注册）。</summary>
    public string Purpose { get; set; } = "register";
}
