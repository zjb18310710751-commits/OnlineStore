using OnlineStore.Models;

namespace OnlineStore.Services;

/// <summary>店铺业务抽象。</summary>
public interface IShopService
{
    Task<IReadOnlyList<Shop>> GetAllAsync();

    Task<Shop?> GetByIdAsync(int id);

    Task<Shop> CreateAsync(Shop shop);

    Task UpdateAsync(Shop shop);

    /// <summary>删除店铺；店铺下仍有商品时返回 false。</summary>
    Task<bool> DeleteAsync(int id);

    Task<int> CountAsync();
}
