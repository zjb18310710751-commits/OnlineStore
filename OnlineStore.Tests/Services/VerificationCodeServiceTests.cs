using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Tests.Services;

/// <summary>邮箱验证码服务单元测试：生成/校验/过期/一次性/冷却。</summary>
public class VerificationCodeServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GenerateAsync_ReturnsSixDigitCode()
    {
        using var context = CreateContext();
        var service = new VerificationCodeService(context);

        var code = await service.GenerateAsync("a@b.com");

        Assert.Matches(@"^\d{6}$", code);
    }

    [Fact]
    public async Task VerifyAsync_CorrectCode_Succeeds()
    {
        using var context = CreateContext();
        var service = new VerificationCodeService(context);
        var code = await service.GenerateAsync("a@b.com");

        Assert.True(await service.VerifyAsync("a@b.com", code));
    }

    [Fact]
    public async Task VerifyAsync_WrongCode_Fails()
    {
        using var context = CreateContext();
        context.EmailVerificationCodes.Add(new EmailVerificationCode
        {
            Email = "a@b.com", Code = "123456", ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        });
        await context.SaveChangesAsync();
        var service = new VerificationCodeService(context);

        Assert.False(await service.VerifyAsync("a@b.com", "654321"));
    }

    [Fact]
    public async Task VerifyAsync_Expired_Fails()
    {
        using var context = CreateContext();
        context.EmailVerificationCodes.Add(new EmailVerificationCode
        {
            Email = "a@b.com", Code = "123456", ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        });
        await context.SaveChangesAsync();
        var service = new VerificationCodeService(context);

        Assert.False(await service.VerifyAsync("a@b.com", "123456"));
    }

    [Fact]
    public async Task VerifyAsync_ConsumesCode_SecondVerifyFails()
    {
        using var context = CreateContext();
        var service = new VerificationCodeService(context);
        var code = await service.GenerateAsync("a@b.com");

        Assert.True(await service.VerifyAsync("a@b.com", code));
        Assert.False(await service.VerifyAsync("a@b.com", code));
    }

    [Fact]
    public async Task GenerateAsync_Cooldown_Throws()
    {
        using var context = CreateContext();
        var service = new VerificationCodeService(context);
        await service.GenerateAsync("a@b.com");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateAsync("a@b.com"));
    }
}
