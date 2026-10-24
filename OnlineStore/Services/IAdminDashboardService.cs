namespace OnlineStore.Services;

public sealed record DailyRevenuePoint(string Label, decimal Revenue);

public sealed record CategoryRevenuePoint(string Name, decimal Revenue);

public sealed class AdminDashboardData
{
    public decimal TodayRevenue { get; init; }

    public decimal YesterdayRevenue { get; init; }

    public int TodayOrderCount { get; init; }

    public decimal TodayAverageOrderValue { get; init; }

    public decimal TodayRefundAmount { get; init; }

    public IReadOnlyList<DailyRevenuePoint> Trend { get; init; } =
        Array.Empty<DailyRevenuePoint>();

    public IReadOnlyDictionary<string, int> OrderStatusCounts { get; init; } =
        new Dictionary<string, int>();

    public IReadOnlyList<CategoryRevenuePoint> CategoryRevenues { get; init; } =
        Array.Empty<CategoryRevenuePoint>();

    public int PendingShipmentCount { get; init; }

    public int LowStockCount { get; init; }
}

public interface IAdminDashboardService
{
    Task<AdminDashboardData> GetAsync();
}
