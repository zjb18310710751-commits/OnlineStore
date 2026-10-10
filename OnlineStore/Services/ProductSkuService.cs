using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;

namespace OnlineStore.Services;

public class ProductSkuService : IProductSkuService
{
    private readonly ApplicationDbContext _context;

    public ProductSkuService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProductSku>> GetByProductIdAsync(int productId) =>
        await _context.ProductSkus
            .AsNoTracking()
            .Where(s => s.ProductId == productId)
            .OrderBy(s => s.Color)
            .ThenBy(s => s.Spec)
            .ToListAsync();

    public Task<ProductSku?> GetByIdAsync(int id) =>
        _context.ProductSkus.FirstOrDefaultAsync(s => s.Id == id);

    public async Task<ProductSku> CreateAsync(ProductSku sku)
    {
        _context.ProductSkus.Add(sku);
        await _context.SaveChangesAsync();
        return sku;
    }

    public async Task UpdateAsync(ProductSku sku)
    {
        _context.ProductSkus.Update(sku);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var sku = await _context.ProductSkus.FirstOrDefaultAsync(s => s.Id == id);
        if (sku is not null)
        {
            _context.ProductSkus.Remove(sku);
            await _context.SaveChangesAsync();
        }
    }
}
