using OnlineStore.Models;

namespace OnlineStore.ViewModels;

public sealed record AdminOrderUpdateViewModel(
    int Id, string Status, string StatusLabel, string? ReceiptNote, bool IsReceiptException)
{
    public static AdminOrderUpdateViewModel FromOrder(Order order)
    {
        var isException = order.Status == OrderStatus.ReceiptException;
        string? note = null;
        if (isException && !string.IsNullOrWhiteSpace(order.ReceiptExceptionReason))
        {
            note = $"{L10n.Text("签收异常", "Receipt exception")}：{order.ReceiptExceptionReason}"
                + $" · {order.ReceiptExceptionReportedBy} · {order.ReceiptExceptionReportedAt?.ToLocalTime():yyyy-MM-dd HH:mm}";
        }
        else if (order.Status == OrderStatus.Completed && order.ReceiptConfirmedAutomatically)
        {
            note = L10n.Text("确认期届满，系统自动确认收货", "Auto-confirmed after the 7-day window");
        }

        return new(order.Id, order.Status, L10n.Status(order.Status), note, isException);
    }
}
