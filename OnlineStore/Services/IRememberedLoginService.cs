namespace OnlineStore.Services;

/// <summary>记住的登录凭据（邮箱与密码，仅用于回填登录表单）。</summary>
public sealed record RememberedLogin(string Email, string Password);

/// <summary>
/// 记住账号密码功能：把邮箱与密码加密后写入持久化 Cookie，供下次登录页回填。
/// 接口抽象便于替换存储实现（如改用加密的 LocalStorage / 数据库）—— 可修改性。
/// </summary>
public interface IRememberedLoginService
{
    /// <summary>保存（或覆盖）记住的登录凭据。</summary>
    void Save(string email, string password);

    /// <summary>读取记住的登录凭据；不存在或已损坏时返回 null。</summary>
    RememberedLogin? Load();

    /// <summary>清除记住的登录凭据。</summary>
    void Clear();
}
