using OnlineStore.Models;

namespace OnlineStore.ViewModels;

public class AdminOrdersViewModel
{
    public string ActiveSection { get; set; } = "orders";

    public IReadOnlyList<Order> Orders { get; set; } = Array.Empty<Order>();

    public IReadOnlyList<AfterSalesRequest> AfterSales { get; set; } = Array.Empty<AfterSalesRequest>();
}
