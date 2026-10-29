using OnlineStore.Models;

namespace OnlineStore.ViewModels;

public sealed record CustomerOrderUpdateViewModel(
    int Id,
    string Status,
    string StatusLabel,
    string StateKey)
{
    public static CustomerOrderUpdateViewModel FromOrder(Order order) =>
        new(order.Id, order.Status, L10n.Status(order.Status), CreateStateKey(order));

    public static string CreateStateKey(Order order)
    {
        var afterSalesState = string.Join(",", order.AfterSalesRequests
            .OrderBy(request => request.Id)
            .Select(request => $"{request.Id}:{request.Status}"));
        return $"{order.Status}|{afterSalesState}";
    }
}
