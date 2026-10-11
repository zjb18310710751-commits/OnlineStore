using OnlineStore.Models;

namespace OnlineStore.Services;

/// <summary>订单业务抽象。</summary>
public interface IOrderService
{
    Task<Order?> GetByIdAsync(int id);

    Task<IReadOnlyList<Order>> GetByUserAsync(string userId);

    /// <summary>后台订单列表（含明细，按时间倒序）。</summary>
    Task<IReadOnlyList<Order>> GetAllAsync();

    Task<Order> CreateAsync(Order order);

    /// <summary>取消订单（待发货→已取消，恢复库存）。</summary>
    Task<bool> CancelAsync(int id);

    /// <summary>退款（已发货/已完成→已退款，恢复库存）。</summary>
    Task<bool> RefundAsync(int id);

    /// <summary>发货（待发货→已发货）。</summary>
    Task<bool> ShipAsync(int id);

    /// <summary>完成（已发货→已完成）。</summary>
    Task<bool> CompleteAsync(int id);

    Task<int> CountAsync();
}
