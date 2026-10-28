using OnlineStore.Models;

namespace OnlineStore.Services;

public interface IFavoriteService
{
    Task<bool> IsFavoriteAsync(string userId, int productId);

    Task<bool> ToggleAsync(string userId, int productId);

    Task<int> CountAsync(string userId);

    Task<IReadOnlyList<Product>> GetFavoritesAsync(string userId);
}
