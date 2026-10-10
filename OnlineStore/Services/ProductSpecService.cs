using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;

namespace OnlineStore.Services;

public class ProductSpecService : IProductSpecService
{
    private readonly ApplicationDbContext _context;

    public ProductSpecService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProductSpec>> GetByProductIdAsync(int productId) =>
        await _context.ProductSpecs
            .AsNoTracking()
            .Where(s => s.ProductId == productId)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Id)
            .ToListAsync();

    public Task<ProductSpec?> GetByIdAsync(int id) =>
        _context.ProductSpecs.FirstOrDefaultAsync(s => s.Id == id);

    public async Task<ProductSpec> CreateAsync(ProductSpec spec)
    {
        _context.ProductSpecs.Add(spec);
        await _context.SaveChangesAsync();
        return spec;
    }

    public async Task UpdateAsync(ProductSpec spec)
    {
        _context.ProductSpecs.Update(spec);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var spec = await _context.ProductSpecs.FirstOrDefaultAsync(s => s.Id == id);
        if (spec is not null)
        {
            _context.ProductSpecs.Remove(spec);
            await _context.SaveChangesAsync();
        }
    }
}
