namespace OnlineStore.Services;

/// <summary>验证码（邮箱或手机号）生成与校验抽象。</summary>
public interface IVerificationCodeService
{
    /// <summary>为目标地址（邮箱或手机号）生成并存储验证码，返回 6 位数字码。</summary>
    Task<string> GenerateAsync(string target);

    /// <summary>校验验证码是否正确且未过期；成功后消费该码（一次性）。</summary>
    Task<bool> VerifyAsync(string target, string code);
}
