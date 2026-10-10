using OnlineStore.Models;

namespace OnlineStore.Services;

/// <summary>购物车业务抽象：加入、查看、更新数量、移除、清空。</summary>
public interface ICartService
{
    /// <summary>读取当前购物车。</summary>
    Task<Cart> GetCartAsync();

    /// <summary>加入商品（可指定规格变体）；已存在则累加数量，且不超过库存上限。</summary>
    Task AddAsync(int productId, int quantity = 1, int? skuId = null);

    /// <summary>更新某一行（商品+规格）的数量；数量为 0 时移除。</summary>
    Task UpdateAsync(int productId, int? skuId, int quantity);

    /// <summary>移除某一行（商品+规格）。</summary>
    Task RemoveAsync(int productId, int? skuId);

    /// <summary>清空购物车。</summary>
    Task ClearAsync();
}
