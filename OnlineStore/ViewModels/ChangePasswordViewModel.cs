namespace OnlineStore.ViewModels;

/// <summary>个人中心修改邮箱与密码：不要求当前密码，通过邮箱验证码确认身份。</summary>
public class ChangePasswordViewModel : ProfileSidebarViewModel
{
    public string? RegisteredEmail { get; set; }

    public string? VerificationCode { get; set; }

    public string? NewPassword { get; set; }

    public string? ConfirmPassword { get; set; }
}
