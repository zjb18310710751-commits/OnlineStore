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

    Task<bool> ShipAsync(int id, string? carrier, string? trackingNumber);

    Task<bool> CompleteAsync(int id);

    Task<OrderTrackingView?> GetTrackingAsync(int id);

    Task<bool> ConfirmReceiptAsync(int id, string userId);

    Task<bool> DeleteForUserAsync(int id, string userId);

    Task<AfterSalesRequest> CreateAfterSalesAsync(int orderId, string userId, string type, string reason);

    Task<IReadOnlyList<AfterSalesRequest>> GetAfterSalesByUserAsync(string userId);

    Task<IReadOnlyList<AfterSalesRequest>> GetAfterSalesAllAsync();

    Task<bool> ReviewAfterSalesAsync(int id, bool approved, string? note);

    Task<bool> MarkAfterSalesReceivedAsync(int id);

    Task<bool> CompleteAfterSalesAsync(int id);

    Task<int> CountAsync();
}
