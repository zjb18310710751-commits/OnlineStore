using OnlineStore.Models;

namespace OnlineStore.Services;

public sealed record DeliveryAddressInput(
    string? RecipientName,
    string? PhoneNumber,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? PostalCode,
    string? Country,
    bool IsDefault);

public interface IDeliveryAddressService
{
    Task<IReadOnlyList<DeliveryAddress>> GetAsync(string userId);

    Task<DeliveryAddress?> GetDefaultAsync(string userId);

    Task EnsureLegacyAddressAsync(ApplicationUser user);

    Task<DeliveryAddress?> SaveAsync(string userId, int? id, DeliveryAddressInput input);

    Task<bool> DeleteAsync(string userId, int id);

    Task<bool> SetDefaultAsync(string userId, int id);
}
