using OnlineStore.Models;

namespace OnlineStore.Services;

public interface IOrderService
{
    Task<Order?> GetByIdAsync(int id);

    Task<IReadOnlyList<Order>> GetByUserAsync(string userId);

    Task<IReadOnlyList<Order>> GetAllAsync();

    Task<Order> CreateAsync(Order order);

    Task<bool> CancelAsync(int id);

    Task<bool> RefundAsync(int id);

    Task<bool> ShipAsync(int id);

    Task<bool> CompleteAsync(int id);

    Task<int> CountAsync();
}
