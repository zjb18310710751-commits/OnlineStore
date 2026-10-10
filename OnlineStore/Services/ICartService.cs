using OnlineStore.Models;

namespace OnlineStore.Services;

/// <summary>购物车业务抽象：加入、查看、更新数量、移除、清空。</summary>
public interface ICartService
{
    /// <summary>读取当前购物车。</summary>
    Task<Cart> GetCartAsync();

    /// <summary>加入商品；已存在则累加数量，且不超过库存上限。</summary>
    Task AddAsync(int productId, int quantity = 1);

    /// <summary>更新某商品数量；数量为 0 时移除该商品。</summary>
    Task UpdateAsync(int productId, int quantity);

    /// <summary>移除某商品。</summary>
    Task RemoveAsync(int productId);

    /// <summary>清空购物车。</summary>
    Task ClearAsync();
}
