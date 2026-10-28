using OnlineStore.Models;

namespace OnlineStore.ViewModels;

public class AdminOrdersViewModel
{
    public string ActiveSection { get; set; } = "orders";

    public IReadOnlyList<Order> Orders { get; set; } = Array.Empty<Order>();

    public IReadOnlyList<AfterSalesRequest> AfterSales { get; set; } = Array.Empty<AfterSalesRequest>();

    public int OrdersPage { get; set; } = 1;
    public int OrdersTotalPages { get; set; } = 1;
    public int TotalOrderCount { get; set; }
    public int AfterSalesPage { get; set; } = 1;
    public int AfterSalesTotalPages { get; set; } = 1;
    public int PendingAfterSalesCount { get; set; }
}
