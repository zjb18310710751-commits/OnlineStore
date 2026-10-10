using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.ViewModels;

namespace OnlineStore.Services;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;

    public ProductService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductListViewModel> GetPagedAsync(ProductQuery query)
    {
        var q = _context.Products.AsNoTracking().Where(p => p.IsActive);

        if (query.CategoryId.HasValue)
        {
            q = q.Where(p => p.CategoryId == query.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim();
            q = q.Where(p => p.Name.Contains(term)
                          || (p.Description != null && p.Description.Contains(term))
                          || (p.Sku != null && p.Sku.Contains(term)));
        }

        if (query.MinPrice.HasValue)
        {
            q = q.Where(p => p.Price >= query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            q = q.Where(p => p.Price <= query.MaxPrice.Value);
        }

        q = query.SortBy switch
        {
            "price_asc" => q.OrderBy(p => p.Price),
            "price_desc" => q.OrderByDescending(p => p.Price),
            "name" => q.OrderBy(p => p.Name),
            _ => q.OrderByDescending(p => p.CreatedAt) // 默认：最新上架
        };

        var total = await q.CountAsync();
        var pageSize = query.PageSize > 0 ? query.PageSize : 12;
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        var page = Math.Clamp(query.Page, 1, Math.Max(1, totalPages));

        var items = await q
            .Include(p => p.Category)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

        return new ProductListViewModel
        {
            Items = items,
            Categories = categories,
            Query = query,
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            TotalPages = totalPages
        };
    }

    public Task<Product?> GetByIdAsync(int id) =>
        _context.Products.FirstOrDefaultAsync(p => p.Id == id);

    public Task<Product?> GetByIdWithCategoryAsync(int id) =>
        _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<(IReadOnlyList<Product> Items, int Total)> GetAdminPagedAsync(
        string? search, int? categoryId, int page, int pageSize)
    {
        var q = _context.Products.AsNoTracking().Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(p => p.Name.Contains(term) || (p.Sku != null && p.Sku.Contains(term)));
        }

        if (categoryId.HasValue)
        {
            q = q.Where(p => p.CategoryId == categoryId.Value);
        }

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(p => p.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Product> CreateAsync(Product product)
    {
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task UpdateAsync(Product product)
    {
        product.UpdatedAt = DateTime.UtcNow;
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is not null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
    }

    public Task<int> CountAsync() => _context.Products.CountAsync();

    public Task<int> CountActiveAsync() => _context.Products.CountAsync(p => p.IsActive);

    public Task<int> CountLowStockAsync(int threshold = 5) =>
        _context.Products.CountAsync(p => p.Stock < threshold);
}
