using OnlineStore.Models;

namespace OnlineStore.Services;

/// <summary>
/// 购物车存储抽象。当前实现基于 Session（<see cref="SessionCartStore"/>），
/// 后续可替换为数据库或 Redis 实现而无需改动业务逻辑（可修改性）。
/// </summary>
public interface ICartStore
{
    Task<List<CartItem>> GetAsync();

    Task SaveAsync(List<CartItem> items);
}
