using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Tests.Services;

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
    public async Task GenerateAsync_Phone_ReturnsSixDigitCode()
    {
        using var context = CreateContext();
        var service = new VerificationCodeService(context);

        var code = await service.GenerateAsync("13800138000");

        Assert.Matches(@"^\d{6}$", code);
    }

    [Fact]
    public async Task VerifyAsync_Phone_CorrectCode_Succeeds()
    {
        using var context = CreateContext();
        var service = new VerificationCodeService(context);
        var code = await service.GenerateAsync("13800138000");

        Assert.True(await service.VerifyAsync("13800138000", code));
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

    [Fact]
    public async Task VerifyAsync_FiveWrongAttempts_BlockCorrectCodeAcrossRequests()
    {
        var options = await CreateSharedCodeAsync();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await using var request = new ApplicationDbContext(options);
            Assert.False(await new VerificationCodeService(request).VerifyAsync("a@b.com", "000000"));
        }

        await using var finalRequest = new ApplicationDbContext(options);
        Assert.False(await new VerificationCodeService(finalRequest).VerifyAsync("a@b.com", "123456"));
    }

    [Fact]
    public async Task VerifyAsync_FourWrongAttempts_StillAllowsTheCorrectFifthAttempt()
    {
        var options = await CreateSharedCodeAsync();
        await using var request = new ApplicationDbContext(options);
        var service = new VerificationCodeService(request);
        for (var attempt = 0; attempt < 4; attempt++)
        {
            Assert.False(await service.VerifyAsync("a@b.com", "000000"));
        }

        Assert.True(await service.VerifyAsync("a@b.com", "123456"));
        Assert.False(await service.VerifyAsync("a@b.com", "123456"));
    }

    [Fact]
    public async Task GenerateAsync_AfterLockoutAndCooldown_IssuesANewUsableCode()
    {
        var options = await CreateSharedCodeAsync();
        await using var request = new ApplicationDbContext(options);
        var service = new VerificationCodeService(request);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            Assert.False(await service.VerifyAsync("a@b.com", "000000"));
        }
        Assert.False(await service.VerifyAsync("a@b.com", "123456"));
        var previous = await request.EmailVerificationCodes.SingleAsync();
        previous.CreatedAt = DateTime.UtcNow.AddSeconds(-61);
        await request.SaveChangesAsync();

        var replacement = await service.GenerateAsync("a@b.com");

        Assert.True(await service.VerifyAsync("a@b.com", replacement));
    }

    [Fact]
    public async Task VerifyAsync_ConcurrentWrongAttempts_CannotResetTheAttemptBudget()
    {
        var options = await CreateSharedCodeAsync();
        var rendezvous = new SaveRendezvous(12);
        var results = await Task.WhenAll(Enumerable.Range(0, 12).Select(async _ =>
        {
            var concurrentOptions = new DbContextOptionsBuilder<ApplicationDbContext>(options)
                .AddInterceptors(new FirstCodeSaveInterceptor(rendezvous)).Options;
            await using var request = new ApplicationDbContext(concurrentOptions);
            return await new VerificationCodeService(request).VerifyAsync("a@b.com", "000000");
        })).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.All(results, Assert.False);
        await using var finalRequest = new ApplicationDbContext(options);
        Assert.False(await new VerificationCodeService(finalRequest).VerifyAsync("a@b.com", "123456"));
    }

    [Fact]
    public async Task VerifyAsync_ConcurrentCorrectAttempts_ConsumeTheCodeOnlyOnce()
    {
        var options = await CreateSharedCodeAsync();
        var rendezvous = new SaveRendezvous(8);
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
        {
            var concurrentOptions = new DbContextOptionsBuilder<ApplicationDbContext>(options)
                .AddInterceptors(new FirstCodeSaveInterceptor(rendezvous)).Options;
            await using var request = new ApplicationDbContext(concurrentOptions);
            return await new VerificationCodeService(request).VerifyAsync("a@b.com", "123456");
        })).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Single(results.Where(result => result));
    }

    private static async Task<DbContextOptions<ApplicationDbContext>> CreateSharedCodeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new ApplicationDbContext(options);
        context.EmailVerificationCodes.Add(new EmailVerificationCode
        {
            Email = "a@b.com", Code = "123456", ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        });
        await context.SaveChangesAsync();
        return options;
    }

    private sealed class SaveRendezvous(int participants)
    {
        private int _arrived;
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task ArriveAsync()
        {
            if (Interlocked.Increment(ref _arrived) == participants) _ready.TrySetResult();
            return _ready.Task;
        }
    }

    private sealed class FirstCodeSaveInterceptor(SaveRendezvous rendezvous) : SaveChangesInterceptor
    {
        private bool _arrived;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (!_arrived && eventData.Context!.ChangeTracker.Entries<EmailVerificationCode>()
                .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            {
                _arrived = true;
                await rendezvous.ArriveAsync();
            }
            return result;
        }
    }
}
