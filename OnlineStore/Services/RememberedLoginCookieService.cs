using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;

namespace OnlineStore.Services;

/// <summary>
/// 记住账号密码的实现：把邮箱与密码用 Data Protection 加密后写入 HttpOnly 持久化 Cookie，
/// 下次访问登录页时读取并回填表单。密码不以明文存储（安全性）。
/// </summary>
public class RememberedLoginCookieService : IRememberedLoginService
{
    private const string CookieName = "OnlineStore.RememberedLogin";
    private const string Purpose = "OnlineStore.RememberedLogin.v1";
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDataProtector _protector;

    public RememberedLoginCookieService(
        IHttpContextAccessor httpContextAccessor,
        IDataProtectionProvider dataProtectionProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _protector = dataProtectionProvider.CreateProtector(Purpose);
    }

    public void Save(string email, string password)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(new RememberedLogin(email, password));
        var protectedPayload = _protector.Protect(payload);

        httpContext.Response.Cookies.Append(CookieName, protectedPayload, new CookieOptions
        {
            HttpOnly = true,
            Secure = httpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.Add(Lifetime)
        });
    }

    public RememberedLogin? Load()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        if (!httpContext.Request.Cookies.TryGetValue(CookieName, out var protectedPayload))
        {
            return null;
        }

        try
        {
            var payload = _protector.Unprotect(protectedPayload);
            return JsonSerializer.Deserialize<RememberedLogin>(payload);
        }
        catch
        {
            // 数据被篡改、密钥轮换或反序列化失败时，按未记住处理（安全性）
            return null;
        }
    }

    public void Clear()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        httpContext.Response.Cookies.Delete(CookieName);
    }
}
