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
    private const int MaximumAttempts = 5;

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
        if (string.IsNullOrWhiteSpace(target) || code is null || code.Length != 6)
        {
            return false;
        }

        var normalized = target.Trim().ToLowerInvariant();
        EmailVerificationCode? record = null;
        for (var retry = 0; retry < MaximumAttempts; retry++)
        {
            record = await _context.EmailVerificationCodes.AsNoTracking()
                .Where(c => c.Email == normalized)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id)
                .FirstOrDefaultAsync();
            if (record is null || record.ExpiresAt <= DateTime.UtcNow || record.FailedAttempts >= MaximumAttempts)
            {
                return false;
            }

            TrackCode(record);
            record.FailedAttempts++;
            try
            {
                await _context.SaveChangesAsync();
                break;
            }
            catch (DbUpdateConcurrencyException)
            {
                _context.Entry(record).State = EntityState.Detached;
                record = null;
            }
        }

        if (record is null) return false;

        var expected = Encoding.UTF8.GetBytes(record.Code);
        var actual = Encoding.UTF8.GetBytes(code);
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            return false;
        }

        for (var retry = 0; retry < MaximumAttempts; retry++)
        {
            if (record.ExpiresAt <= DateTime.UtcNow) return false;
            _context.EmailVerificationCodes.Remove(record);
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                var id = record.Id;
                _context.Entry(record).State = EntityState.Detached;
                record = await _context.EmailVerificationCodes.SingleOrDefaultAsync(item => item.Id == id);
                if (record is null) return false;
            }
        }

        return false;
    }

    private void TrackCode(EmailVerificationCode record)
    {
        var tracked = _context.EmailVerificationCodes.Local.FirstOrDefault(item => item.Id == record.Id);
        if (tracked is not null) _context.Entry(tracked).State = EntityState.Detached;
        _context.EmailVerificationCodes.Attach(record);
    }
}
