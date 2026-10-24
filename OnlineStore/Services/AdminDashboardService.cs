using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;

namespace OnlineStore.Services;

public class AdminDashboardService : IAdminDashboardService
{
    private static readonly HashSet<string> ValidRevenueStatuses = new(StringComparer.Ordinal)
    {
        OrderStatus.PendingShipment,
        OrderStatus.Shipped,
        OrderStatus.Completed
    };

    private readonly ApplicationDbContext _context;
    private readonly IProductService _productService;

    public AdminDashboardService(
        ApplicationDbContext context,
        IProductService productService)
    {
        _context = context;
        _productService = productService;
    }

    public async Task<AdminDashboardData> GetAsync()
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ToListAsync();

        var today = DateTime.Now.Date;
        var revenueOrders = orders
            .Where(o => ValidRevenueStatuses.Contains(o.Status))
            .ToList();

        var todayRevenue = revenueOrders
            .Where(o => o.CreatedAt.ToLocalTime().Date == today)
            .Sum(o => o.Total);
        var yesterdayRevenue = revenueOrders
            .Where(o => o.CreatedAt.ToLocalTime().Date == today.AddDays(-1))
            .Sum(o => o.Total);
        var todayOrders = revenueOrders.Count(o => o.CreatedAt.ToLocalTime().Date == today);

        var trend = Enumerable.Range(0, 30)
            .Select(offset => today.AddDays(offset - 29))
            .Select(date => new DailyRevenuePoint(
                date.ToString("MM-dd"),
                revenueOrders
                    .Where(o => o.CreatedAt.ToLocalTime().Date == date)
                    .Sum(o => o.Total)))
            .ToList();

        var statusCounts = orders
            .GroupBy(o => o.Status)
            .ToDictionary(group => group.Key, group => group.Count());

        var categoryRevenues = await BuildCategoryRevenuesAsync(revenueOrders, today);

        return new AdminDashboardData
        {
            TodayRevenue = todayRevenue,
            YesterdayRevenue = yesterdayRevenue,
            TodayOrderCount = todayOrders,
            TodayAverageOrderValue = todayOrders == 0 ? 0m : todayRevenue / todayOrders,
            TodayRefundAmount = orders
                .Where(o => o.Status == OrderStatus.Refunded &&
                             o.CreatedAt.ToLocalTime().Date == today)
                .Sum(o => o.Total),
            Trend = trend,
            OrderStatusCounts = statusCounts,
            CategoryRevenues = categoryRevenues,
            PendingShipmentCount = orders.Count(o => o.Status == OrderStatus.PendingShipment),
            LowStockCount = await _productService.CountLowStockAsync()
        };
    }

    private async Task<IReadOnlyList<CategoryRevenuePoint>> BuildCategoryRevenuesAsync(
        IReadOnlyList<Order> revenueOrders,
        DateTime today)
    {
        var start = today.AddDays(-29);
        var productIds = revenueOrders
            .Where(o => o.CreatedAt.ToLocalTime().Date >= start)
            .SelectMany(o => o.Items)
            .Select(i => i.ProductId)
            .Distinct()
            .ToList();
        if (productIds.Count == 0)
        {
            return Array.Empty<CategoryRevenuePoint>();
        }

        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Categories)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();
        var productsById = products.ToDictionary(p => p.Id);
        var categoryTotals = new Dictionary<int, decimal>();
        var categoryNames = new Dictionary<int, string>();

        foreach (var order in revenueOrders.Where(o => o.CreatedAt.ToLocalTime().Date >= start))
        {
            foreach (var item in order.Items)
            {
                if (!productsById.TryGetValue(item.ProductId, out var product))
                {
                    continue;
                }

                foreach (var category in product.Categories)
                {
                    categoryTotals[category.Id] =
                        categoryTotals.GetValueOrDefault(category.Id) + item.LineTotal;
                    categoryNames.TryAdd(category.Id, L10n.Name(category));
                }
            }
        }

        return categoryTotals
            .OrderByDescending(kv => kv.Value)
            .Take(6)
            .Select(kv => new CategoryRevenuePoint(
                categoryNames.GetValueOrDefault(kv.Key, "—"),
                kv.Value))
            .ToList();
    }
}
