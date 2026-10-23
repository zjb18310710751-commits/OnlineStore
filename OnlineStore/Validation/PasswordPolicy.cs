namespace OnlineStore.Validation;

/// <summary>与 Identity 配置一致的密码规则，供注册、找回密码和个人中心共用。</summary>
public static class PasswordPolicy
{
    public const string Pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9]).{6,100}$";
}
