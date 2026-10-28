using OnlineStore.Models;
using OnlineStore.ViewModels;

namespace OnlineStore.Services;

public interface IOrderService
{
    Task<Order?> GetByIdAsync(int id);

    Task<IReadOnlyList<Order>> GetByUserAsync(string userId);

    Task<IReadOnlyList<Order>> GetAllAsync();
    Task<PagedResult<Order>> GetAdminOrdersPagedAsync(int page, int pageSize);

    Task<Order> CreateAsync(Order order);

    Task<bool> CancelAsync(int id);

    Task<bool> RefundAsync(int id);

    Task<bool> ShipAsync(int id, string? carrier, string? trackingNumber);

    Task<OrderTrackingView?> GetTrackingAsync(int id);

    Task<bool> ConfirmReceiptAsync(int id, string userId);

    Task<bool> ReportReceiptExceptionAsync(int id, string reason, string reportedBy);

    Task<int> ProcessPendingOrderStatesAsync();

    Task<int> GetDeliveredItemCountAsync(string userId);

    Task<bool> DeleteForUserAsync(int id, string userId);

    Task<AfterSalesRequest> CreateAfterSalesAsync(int orderId, string userId, string type, string reason);

    Task<IReadOnlyList<AfterSalesRequest>> GetAfterSalesAllAsync();
    Task<PagedResult<AfterSalesRequest>> GetAfterSalesPagedAsync(int page, int pageSize);
    Task<int> CountPendingAfterSalesAsync();

    Task<bool> ReviewAfterSalesAsync(int id, bool approved, string? note);

    Task<bool> MarkAfterSalesReceivedAsync(int id);

    Task<bool> CompleteAfterSalesAsync(int id);

    Task<int> CountAsync();
}
