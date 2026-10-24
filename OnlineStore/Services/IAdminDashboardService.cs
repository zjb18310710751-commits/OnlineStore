using OnlineStore.Models;

namespace OnlineStore.Services;

public sealed record DailyRevenuePoint(string Label, decimal Revenue);

public sealed record TopProductRow(Product Product, int UnitsSold, decimal Revenue);

public sealed record ReviewRow(Review Review, string ProductName);

public sealed class AdminDashboardData
{
    public decimal TotalRevenue { get; init; }

    public int TotalOrderCount { get; init; }

    public int CustomerCount { get; init; }

    public int UnitsSold { get; init; }

    public int PendingShipmentCount { get; init; }

    public int LowStockCount { get; init; }

    public IReadOnlyList<DailyRevenuePoint> Trend { get; init; } =
        Array.Empty<DailyRevenuePoint>();

    public IReadOnlyList<Order> RecentOrders { get; init; } =
        Array.Empty<Order>();

    public IReadOnlyList<TopProductRow> TopProducts { get; init; } =
        Array.Empty<TopProductRow>();

    public IReadOnlyList<ReviewRow> LatestReviews { get; init; } =
        Array.Empty<ReviewRow>();
}

public interface IAdminDashboardService
{
    Task<AdminDashboardData> GetAsync();
}
