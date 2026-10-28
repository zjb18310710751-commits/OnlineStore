using OnlineStore.Models;
using OnlineStore.ViewModels;

namespace OnlineStore.Services;

public interface IShopService
{
    Task<IReadOnlyList<Shop>> GetAllAsync();
    Task<PagedResult<Shop>> GetPagedAsync(int page, int pageSize);

    Task<Shop?> GetByIdAsync(int id);

    Task<Shop> CreateAsync(Shop shop);

    Task UpdateAsync(Shop shop);

    Task<bool> DeleteAsync(int id);

    Task<int> CountAsync();
}
