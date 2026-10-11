namespace OnlineStore.Services;

/// <summary>邮箱验证码生成与校验抽象。</summary>
public interface IVerificationCodeService
{
    /// <summary>为指定邮箱生成并存储验证码，返回 6 位数字码。</summary>
    Task<string> GenerateAsync(string email);

    /// <summary>校验验证码是否正确且未过期；成功后消费该码（一次性）。</summary>
    Task<bool> VerifyAsync(string email, string code);
}
