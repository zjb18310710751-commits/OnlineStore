using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;

namespace OnlineStore.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;

    public OrderService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Order?> GetByIdAsync(int id) =>
        _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<IReadOnlyList<Order>> GetByUserAsync(string userId) =>
        await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<Order>> GetAllAsync() =>
        await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public async Task<Order> CreateAsync(Order order)
    {
        order.CreatedAt = DateTime.UtcNow;
        _context.Orders.Add(order);

        // 下单扣减库存（与取消/退款时的恢复逻辑对称）
        await DecrementStockAsync(order);

        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<bool> CancelAsync(int id)
    {
        var order = await GetByIdAsync(id);
        if (order is null || order.Status != OrderStatus.PendingShipment)
        {
            return false;
        }

        order.Status = OrderStatus.Cancelled;
        await RestoreStockAsync(order);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RefundAsync(int id)
    {
        var order = await GetByIdAsync(id);
        if (order is null || (order.Status != OrderStatus.Shipped && order.Status != OrderStatus.Completed))
        {
            return false;
        }

        order.Status = OrderStatus.Refunded;
        await RestoreStockAsync(order);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ShipAsync(int id)
    {
        var order = await GetByIdAsync(id);
        if (order is null || order.Status != OrderStatus.PendingShipment)
        {
            return false;
        }

        order.Status = OrderStatus.Shipped;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CompleteAsync(int id)
    {
        var order = await GetByIdAsync(id);
        if (order is null || order.Status != OrderStatus.Shipped)
        {
            return false;
        }

        order.Status = OrderStatus.Completed;
        await _context.SaveChangesAsync();
        return true;
    }

    public Task<int> CountAsync() => _context.Orders.CountAsync();

    /// <summary>下单时扣减商品库存（商品级，不精确到 SKU）。</summary>
    private async Task DecrementStockAsync(Order order)
    {
        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        if (productIds.Count == 0)
        {
            return;
        }

        var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
        foreach (var item in order.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product is not null)
            {
                product.Stock = Math.Max(0, product.Stock - item.Quantity);
            }
        }
    }

    /// <summary>取消/退款时把商品库存加回（商品级，不精确到 SKU）。</summary>
    private async Task RestoreStockAsync(Order order)
    {
        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        if (productIds.Count == 0)
        {
            return;
        }

        var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
        foreach (var item in order.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product is not null)
            {
                product.Stock += item.Quantity;
            }
        }
    }
}
