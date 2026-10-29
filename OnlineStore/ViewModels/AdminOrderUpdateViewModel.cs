using OnlineStore.Models;

namespace OnlineStore.ViewModels;

public sealed record AdminOrderUpdateViewModel(
    int Id,
    string Status,
    string StatusLabel,
    string? ReceiptNote,
    bool IsReceiptException,
    string StateKey)
{
    public static AdminOrderUpdateViewModel FromOrder(Order order)
    {
        var isException = order.Status == OrderStatus.ReceiptException;
        string? note = null;
        var transitRequest = order.AfterSalesRequests
            .Where(request =>
                request.Type == AfterSalesType.TransitCancellation
                && request.Status == AfterSalesStatus.Pending)
            .OrderByDescending(request => request.RequestedAt)
            .FirstOrDefault();
        var activeReturnRequest = order.AfterSalesRequests
            .Where(request =>
                (request.Type is AfterSalesType.Return or AfterSalesType.Exchange)
                && (request.Status is AfterSalesStatus.Pending
                    or AfterSalesStatus.Approved
                    or AfterSalesStatus.Returning
                    or AfterSalesStatus.Received))
            .OrderByDescending(request => request.RequestedAt)
            .FirstOrDefault();
        if (transitRequest is not null)
        {
            note = L10n.Text(
                "运输中退款/拦截申请待审核",
                "Refund and interception request awaiting review");
        }
        else if (activeReturnRequest is not null)
        {
            note = $"{L10n.AfterSalesTypeLabel(activeReturnRequest.Type)}"
                + $" · {L10n.AfterSalesStatusLabel(activeReturnRequest.Status)}";
            if (!string.IsNullOrWhiteSpace(activeReturnRequest.ReturnTrackingNumber))
            {
                note += $" · {L10n.Carrier(activeReturnRequest.ReturnCarrier)}"
                    + $" {activeReturnRequest.ReturnTrackingNumber}";
            }
        }
        else if (isException && !string.IsNullOrWhiteSpace(order.ReceiptExceptionReason))
        {
            note = $"{L10n.Text("签收异常", "Receipt exception")}：{order.ReceiptExceptionReason}"
                + $" · {order.ReceiptExceptionReportedBy} · {order.ReceiptExceptionReportedAt?.ToLocalTime():yyyy-MM-dd HH:mm}";
        }
        else if (order.Status == OrderStatus.Completed && order.ReceiptConfirmedAutomatically)
        {
            note = L10n.Text("确认期届满，系统自动确认收货", "Auto-confirmed after the 7-day window");
        }

        return new(
            order.Id,
            order.Status,
            L10n.Status(order.Status),
            note,
            isException,
            CustomerOrderUpdateViewModel.CreateStateKey(order));
    }
}
