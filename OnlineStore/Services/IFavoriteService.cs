using OnlineStore.Models;

namespace OnlineStore.Services;

/// <summary>收藏（心愿单）业务抽象。</summary>
public interface IFavoriteService
{
    Task<bool> IsFavoriteAsync(string userId, int productId);

    /// <summary>切换收藏状态；返回切换后是否已收藏。</summary>
    Task<bool> ToggleAsync(string userId, int productId);

    /// <summary>读取用户收藏的商品（按收藏时间倒序）。</summary>
    Task<IReadOnlyList<Product>> GetFavoritesAsync(string userId);
}
