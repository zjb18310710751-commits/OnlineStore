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
        OrderStatus.Delivered,
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

        var revenueOrders = orders
            .Where(o => o.ReplacementForOrderId is null && ValidRevenueStatuses.Contains(o.Status))
            .ToList();
        var today = DateTime.Now.Date;

        var trend = Enumerable.Range(0, 30)
            .Select(offset => today.AddDays(offset - 29))
            .Select(date => new DailyRevenuePoint(
                date.ToString("MM-dd"),
                revenueOrders
                    .Where(o => o.CreatedAt.ToLocalTime().Date == date)
                    .Sum(o => o.Total)))
            .ToList();

        var unitsSold = revenueOrders
            .SelectMany(o => o.Items)
            .Sum(i => i.Quantity);

        var topProducts = await BuildTopProductsAsync(revenueOrders);
        var latestReviews = await BuildLatestReviewsAsync();

        return new AdminDashboardData
        {
            TotalRevenue = revenueOrders.Sum(o => o.Total),
            TotalOrderCount = revenueOrders.Count,
            CustomerCount = revenueOrders
                .Where(o => !string.IsNullOrWhiteSpace(o.UserId))
                .Select(o => o.UserId!)
                .Distinct()
                .Count(),
            UnitsSold = unitsSold,
            PendingShipmentCount = orders.Count(o => o.Status == OrderStatus.PendingShipment),
            LowStockCount = await _productService.CountLowStockAsync(),
            Trend = trend,
            RecentOrders = orders
                .OrderByDescending(o => o.CreatedAt)
                .Take(6)
                .ToList(),
            TopProducts = topProducts,
            LatestReviews = latestReviews
        };
    }

    private async Task<IReadOnlyList<TopProductRow>> BuildTopProductsAsync(
        IReadOnlyList<Order> revenueOrders)
    {
        var totals = new Dictionary<int, (int Quantity, decimal Revenue)>();
        foreach (var order in revenueOrders)
        {
            foreach (var item in order.Items)
            {
                if (!totals.TryGetValue(item.ProductId, out var current))
                {
                    current = (0, 0m);
                }

                totals[item.ProductId] = (
                    current.Quantity + item.Quantity,
                    current.Revenue + item.LineTotal);
            }
        }

        if (totals.Count == 0)
        {
            return Array.Empty<TopProductRow>();
        }

        var productIds = totals.Keys.ToList();
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        return totals
            .Where(kv => products.Any(p => p.Id == kv.Key))
            .OrderByDescending(kv => kv.Value.Revenue)
            .Take(5)
            .Select(kv => new TopProductRow(
                products.First(p => p.Id == kv.Key),
                kv.Value.Quantity,
                kv.Value.Revenue))
            .ToList();
    }

    private async Task<IReadOnlyList<ReviewRow>> BuildLatestReviewsAsync()
    {
        var reviews = await _context.Reviews
            .AsNoTracking()
            .Include(r => r.Product)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .ToListAsync();

        return reviews
            .Select(r => new ReviewRow(
                r,
                r.Product is null ? "—" : L10n.Name(r.Product)))
            .ToList();
    }
}
