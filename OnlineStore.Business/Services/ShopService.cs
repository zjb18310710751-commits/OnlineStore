using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.ViewModels;

namespace OnlineStore.Services;

public class ShopService : IShopService
{
    private readonly ApplicationDbContext _context;

    public ShopService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Shop>> GetAllAsync() =>
        await _context.Shops
            .AsNoTracking()
            .OrderByDescending(s => s.Rating)
            .ThenBy(s => s.Name)
            .ToListAsync();

    public async Task<PagedResult<Shop>> GetPagedAsync(int page, int pageSize)
    {
        var query = _context.Shops.AsNoTracking().OrderBy(shop => shop.Name);
        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<Shop>(items, page, pageSize, total);
    }

    public Task<Shop?> GetByIdAsync(int id) =>
        _context.Shops.FirstOrDefaultAsync(s => s.Id == id);

    public async Task<Shop> CreateAsync(Shop shop)
    {
        _context.Shops.Add(shop);
        await _context.SaveChangesAsync();
        return shop;
    }

    public async Task UpdateAsync(Shop shop)
    {
        _context.Shops.Update(shop);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var shop = await _context.Shops
            .Include(s => s.Products)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (shop is null)
        {
            return false;
        }

        if (shop.Products.Any())
        {
            return false;
        }

        _context.Shops.Remove(shop);
        await _context.SaveChangesAsync();
        return true;
    }

}
