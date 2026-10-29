using OnlineStore.Models;

namespace OnlineStore.Services;

public interface ICartStore
{
    Task<List<CartItem>> GetAsync();

    Task SaveAsync(List<CartItem> items);
}
