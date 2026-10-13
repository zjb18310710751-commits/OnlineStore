using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using OnlineStore.Services;

namespace OnlineStore.Tests.Services;

/// <summary>记住账号密码服务单元测试：保存/读取/清除，以及密码不以明文落盘。</summary>
public class RememberedLoginCookieServiceTests
{
    private const string CookieName = "OnlineStore.RememberedLogin";

    private static (HttpContextAccessor Accessor, EphemeralDataProtectionProvider Protector) CreateAccessor(DefaultHttpContext context)
    {
        var accessor = new HttpContextAccessor { HttpContext = context };
        var protector = new EphemeralDataProtectionProvider();
        return (accessor, protector);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsEmailAndPassword()
    {
        var context = new DefaultHttpContext();
        var (accessor, protector) = CreateAccessor(context);
        var service = new RememberedLoginCookieService(accessor, protector);

        service.Save("user@store.com", "Passw0rd@123");

        // 模拟浏览器把响应 Set-Cookie 回传给下一次请求
        var setCookie = context.Response.Headers["Set-Cookie"].ToString();
        context.Request.Headers["Cookie"] = setCookie.Split(';')[0];

        var loaded = service.Load();

        Assert.NotNull(loaded);
        Assert.Equal("user@store.com", loaded!.Email);
        Assert.Equal("Passw0rd@123", loaded.Password);
    }

    [Fact]
    public void Load_NoCookie_ReturnsNull()
    {
        var context = new DefaultHttpContext();
        var (accessor, protector) = CreateAccessor(context);
        var service = new RememberedLoginCookieService(accessor, protector);

        Assert.Null(service.Load());
    }

    [Fact]
    public void Load_TamperedCookie_ReturnsNull()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["Cookie"] = $"{CookieName}=tampered-value";
        var (accessor, protector) = CreateAccessor(context);
        var service = new RememberedLoginCookieService(accessor, protector);

        Assert.Null(service.Load());
    }

    [Fact]
    public void Save_DoesNotExposePasswordInPlaintext()
    {
        var context = new DefaultHttpContext();
        var (accessor, protector) = CreateAccessor(context);
        var service = new RememberedLoginCookieService(accessor, protector);

        service.Save("user@store.com", "Passw0rd@123");

        var setCookie = context.Response.Headers["Set-Cookie"].ToString();
        Assert.DoesNotContain("Passw0rd@123", setCookie);
        Assert.DoesNotContain("user@store.com", setCookie);
    }
}
