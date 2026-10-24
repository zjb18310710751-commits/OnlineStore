using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;

namespace OnlineStore.Services;

public class DeliveryAddressService : IDeliveryAddressService
{
    private readonly ApplicationDbContext _context;

    public DeliveryAddressService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<DeliveryAddress>> GetAsync(string userId) =>
        await _context.DeliveryAddresses
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.CreatedAt)
            .ToListAsync();

    public Task<DeliveryAddress?> GetDefaultAsync(string userId) =>
        _context.DeliveryAddresses
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.CreatedAt)
            .FirstOrDefaultAsync();

    public async Task EnsureLegacyAddressAsync(ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(user.Address) ||
            await _context.DeliveryAddresses.AnyAsync(a => a.UserId == user.Id))
        {
            return;
        }

        var parts = user.Address.Split(", ", 5, StringSplitOptions.None);
        var recipientName = $"{user.LastName}{user.FirstName}".Trim();
        _context.DeliveryAddresses.Add(new DeliveryAddress
        {
            UserId = user.Id,
            RecipientName = string.IsNullOrWhiteSpace(recipientName)
                ? user.UserName ?? "—"
                : recipientName,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            AddressLine1 = parts.ElementAtOrDefault(0) ?? user.Address,
            AddressLine2 = parts.ElementAtOrDefault(1),
            City = parts.ElementAtOrDefault(2) ?? string.Empty,
            PostalCode = parts.ElementAtOrDefault(3) ?? string.Empty,
            Country = parts.ElementAtOrDefault(4) ?? string.Empty,
            IsDefault = true
        });
        await _context.SaveChangesAsync();
    }

    public async Task<DeliveryAddress?> SaveAsync(
        string userId,
        int? id,
        DeliveryAddressInput input)
    {
        var addresses = await _context.DeliveryAddresses
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();

        var address = id.HasValue
            ? addresses.SingleOrDefault(a => a.Id == id.Value)
            : null;
        if (id.HasValue && address is null)
        {
            return null;
        }

        if (address is null)
        {
            address = new DeliveryAddress
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            addresses.Add(address);
            _context.DeliveryAddresses.Add(address);
        }

        address.RecipientName = input.RecipientName!.Trim();
        address.PhoneNumber = input.PhoneNumber!.Trim();
        address.AddressLine1 = input.AddressLine1!.Trim();
        address.AddressLine2 = NullIfBlank(input.AddressLine2);
        address.City = input.City!.Trim();
        address.PostalCode = input.PostalCode!.Trim();
        address.Country = input.Country!.Trim();
        address.UpdatedAt = DateTime.UtcNow;

        var hasAnotherDefault = addresses.Any(a => a != address && a.IsDefault);
        if (input.IsDefault || !hasAnotherDefault)
        {
            foreach (var item in addresses)
            {
                item.IsDefault = item == address;
            }
        }

        await SynchronizeLegacyAddressAsync(userId, addresses);
        await _context.SaveChangesAsync();
        return address;
    }

    public async Task<bool> DeleteAsync(string userId, int id)
    {
        var addresses = await _context.DeliveryAddresses
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
        var address = addresses.SingleOrDefault(a => a.Id == id);
        if (address is null)
        {
            return false;
        }

        var wasDefault = address.IsDefault;
        addresses.Remove(address);
        _context.DeliveryAddresses.Remove(address);

        if (wasDefault && addresses.Count > 0)
        {
            foreach (var item in addresses)
            {
                item.IsDefault = false;
            }
            addresses[0].IsDefault = true;
        }

        await SynchronizeLegacyAddressAsync(userId, addresses);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetDefaultAsync(string userId, int id)
    {
        var addresses = await _context.DeliveryAddresses
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
        var target = addresses.SingleOrDefault(a => a.Id == id);
        if (target is null)
        {
            return false;
        }

        foreach (var address in addresses)
        {
            address.IsDefault = address == target;
            if (address == target)
            {
                address.UpdatedAt = DateTime.UtcNow;
            }
        }

        await SynchronizeLegacyAddressAsync(userId, addresses);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task SynchronizeLegacyAddressAsync(
        string userId,
        IReadOnlyCollection<DeliveryAddress> addresses)
    {
        var defaultAddress = addresses.FirstOrDefault(a => a.IsDefault) ??
                             addresses.OrderBy(a => a.CreatedAt).FirstOrDefault();

        if (defaultAddress is not null)
        {
            foreach (var address in addresses)
            {
                address.IsDefault = address == defaultAddress;
            }
        }

        var user = await _context.Users.FindAsync(userId);
        if (user is not null)
        {
            user.Address = defaultAddress is null
                ? null
                : AddressFormatter.Combine(
                    defaultAddress.AddressLine1,
                    defaultAddress.AddressLine2,
                    defaultAddress.City,
                    defaultAddress.PostalCode,
                    defaultAddress.Country);
        }
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
