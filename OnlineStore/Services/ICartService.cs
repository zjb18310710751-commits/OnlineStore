using OnlineStore.Models;

namespace OnlineStore.Services;

public interface ICartService
{

    Task<Cart> GetCartAsync();

    Task<Cart> RefreshAsync();

    Task<bool> AddAsync(int productId, int quantity = 1, int? skuId = null);

    Task UpdateAsync(int productId, int? skuId, int quantity);

    Task RemoveAsync(int productId, int? skuId);

    Task ClearAsync();
}
