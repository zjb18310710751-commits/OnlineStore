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

/// <summary>用户收货地址簿业务抽象。</summary>
public interface IDeliveryAddressService
{
    Task<IReadOnlyList<DeliveryAddress>> GetAsync(string userId);

    Task<DeliveryAddress?> GetDefaultAsync(string userId);

    /// <summary>将旧版单地址字段转换为第一条结构化地址，已有结构化地址时不重复创建。</summary>
    Task EnsureLegacyAddressAsync(ApplicationUser user);

    Task<DeliveryAddress?> SaveAsync(string userId, int? id, DeliveryAddressInput input);

    Task<bool> DeleteAsync(string userId, int id);

    Task<bool> SetDefaultAsync(string userId, int id);
}
