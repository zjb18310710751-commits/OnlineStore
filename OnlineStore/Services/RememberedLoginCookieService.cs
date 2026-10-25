using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;

namespace OnlineStore.Services;

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
