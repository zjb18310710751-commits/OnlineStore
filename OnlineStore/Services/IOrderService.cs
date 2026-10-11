using OnlineStore.Models;

namespace OnlineStore.Services;

/// <summary>订单业务抽象。</summary>
public interface IOrderService
{
    Task<Order?> GetByIdAsync(int id);

    Task<IReadOnlyList<Order>> GetByUserAsync(string userId);

    Task<Order> CreateAsync(Order order);

    Task<int> CountAsync();
}
