using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using OnlineStore.Data;
using OnlineStore.Models;

namespace OnlineStore.Services;

public class VerificationCodeService : IVerificationCodeService
{
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan Validity = TimeSpan.FromMinutes(10);

    private readonly ApplicationDbContext _context;
    private readonly IStringLocalizer<SharedResource> _loc;

    public VerificationCodeService(ApplicationDbContext context, IStringLocalizer<SharedResource>? localizer = null)
    {
        _context = context;
        _loc = localizer ?? FallbackStringLocalizer<SharedResource>.Instance;
    }

    public async Task<string> GenerateAsync(string target)
    {

        var normalized = target.Trim().ToLowerInvariant();

        var latest = await _context.EmailVerificationCodes
            .Where(c => c.Email == normalized)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (latest is not null && latest.CreatedAt > DateTime.UtcNow - Cooldown)
        {
            throw new InvalidOperationException(_loc["VerificationCodeTooFrequent"].Value);
        }

        var olds = await _context.EmailVerificationCodes.Where(c => c.Email == normalized).ToListAsync();
        if (olds.Count > 0)
        {
            _context.EmailVerificationCodes.RemoveRange(olds);
        }

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        _context.EmailVerificationCodes.Add(new EmailVerificationCode
        {
            Email = normalized,
            Code = code,
            ExpiresAt = DateTime.UtcNow.Add(Validity)
        });
        await _context.SaveChangesAsync();

        return code;
    }

    public async Task<bool> VerifyAsync(string target, string code)
    {
        var normalized = target.Trim().ToLowerInvariant();
        var record = await _context.EmailVerificationCodes
            .Where(c => c.Email == normalized)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (record is null || record.ExpiresAt <= DateTime.UtcNow)
        {
            return false;
        }

        var expected = Encoding.UTF8.GetBytes(record.Code);
        var actual = Encoding.UTF8.GetBytes(code);
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            return false;
        }

        _context.EmailVerificationCodes.Remove(record);
        await _context.SaveChangesAsync();
        return true;
    }
}
