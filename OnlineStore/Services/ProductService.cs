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
            q = q.Where(p => p.Categories.Any(c => c.Id == query.CategoryId.Value));
        }

        if (query.ShopId.HasValue)
        {
            q = q.Where(p => p.ShopId == query.ShopId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim();
            q = q.Where(p => p.Name.Contains(term)
                          || (p.NameEn != null && p.NameEn.Contains(term))
                          || (p.Description != null && p.Description.Contains(term))
                          || (p.DescriptionEn != null && p.DescriptionEn.Contains(term))
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

        if (!string.IsNullOrWhiteSpace(query.RatingBand))
        {
            q = query.RatingBand switch
            {
                "4.5-5" => q.Where(p => p.Reviews.Any()
                    && p.Reviews.Average(r => (double)r.Rating) >= 4.5),
                "4.0-4.4" => q.Where(p => p.Reviews.Any()
                    && p.Reviews.Average(r => (double)r.Rating) >= 4.0
                    && p.Reviews.Average(r => (double)r.Rating) < 4.5),
                "3.0-3.9" => q.Where(p => p.Reviews.Any()
                    && p.Reviews.Average(r => (double)r.Rating) >= 3.0
                    && p.Reviews.Average(r => (double)r.Rating) < 4.0),
                "2.0-2.9" => q.Where(p => p.Reviews.Any()
                    && p.Reviews.Average(r => (double)r.Rating) >= 2.0
                    && p.Reviews.Average(r => (double)r.Rating) < 3.0),
                "1.0-1.9" => q.Where(p => p.Reviews.Any()
                    && p.Reviews.Average(r => (double)r.Rating) >= 1.0
                    && p.Reviews.Average(r => (double)r.Rating) < 2.0),
                "none" => q.Where(p => !p.Reviews.Any()),
                _ => q
            };
        }

        if (query.InStockOnly)
        {
            q = q.Where(p => p.Stock > 0);
        }

        q = query.SortBy switch
        {
            "price_asc" => q.OrderBy(p => p.Price),
            "price_desc" => q.OrderByDescending(p => p.Price),
            "name" => q.OrderBy(p => p.Name),
            _ => q.OrderByDescending(p => p.CreatedAt)
        };

        var total = await q.CountAsync();
        var pageSize = query.PageSize > 0 ? query.PageSize : 12;
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        var page = Math.Clamp(query.Page, 1, Math.Max(1, totalPages));

        var items = await q
            .Include(p => p.Categories)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        await AttachRatingSummariesAsync(items);

        var recommendations = new List<Product>();
        if (total == 0 && !string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            recommendations = await _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive)
                .Include(p => p.Categories)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync();
            await AttachRatingSummariesAsync(recommendations);
        }

        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

        var brands = await _context.Shops
            .AsNoTracking()
            .Where(s => s.Products.Any(p => p.IsActive))
            .OrderBy(s => s.Name)
            .ToListAsync();

        return new ProductListViewModel
        {
            Items = items,
            Categories = categories,
            Brands = brands,
            Query = query,
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            TotalPages = totalPages,
            Recommendations = recommendations
        };
    }

    public Task<Product?> GetByIdAsync(int id) =>
        _context.Products.FirstOrDefaultAsync(p => p.Id == id);

    public Task<Product?> GetByIdWithDetailsAsync(int id) =>
        _context.Products
            .AsNoTracking()
            .Include(p => p.Shop)
            .Include(p => p.Categories)
            .Include(p => p.Skus)
            .Include(p => p.Specs)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<(IReadOnlyList<Product> Items, int Total)> GetAdminPagedAsync(
        string? search, int? categoryId, int page, int pageSize)
    {
        var q = _context.Products.AsNoTracking().Include(p => p.Shop).Include(p => p.Categories).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(p => p.Name.Contains(term) || (p.NameEn != null && p.NameEn.Contains(term)) || (p.Sku != null && p.Sku.Contains(term)));
        }

        if (categoryId.HasValue)
        {
            q = q.Where(p => p.Categories.Any(c => c.Id == categoryId.Value));
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

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(int productId) =>
        await _context.Products
            .Where(p => p.Id == productId)
            .SelectMany(p => p.Categories)
            .AsNoTracking()
            .ToListAsync();

    public async Task SetCategoriesAsync(Product product, IEnumerable<int> categoryIds)
    {
        var ids = categoryIds.Distinct().ToList();
        var categories = await _context.Categories.Where(c => ids.Contains(c.Id)).ToListAsync();

        var nav = _context.Entry(product).Collection(p => p.Categories);
        if (!nav.IsLoaded)
        {
            await nav.LoadAsync();
        }

        product.Categories.Clear();
        foreach (var c in categories)
        {
            product.Categories.Add(c);
        }
    }

    public async Task<IReadOnlyList<Product>> GetByShopAsync(int shopId)
    {
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.ShopId == shopId && p.IsActive)
            .Include(p => p.Categories)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        await AttachRatingSummariesAsync(products);
        return products;
    }

    public async Task<IReadOnlyList<Product>> GetSaleItemsAsync(int count = 8)
    {
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.OriginalPrice != null && p.OriginalPrice > p.Price)
            .Include(p => p.Categories)
            .OrderByDescending(p => p.UpdatedAt)
            .Take(count)
            .ToListAsync();
        await AttachRatingSummariesAsync(products);
        return products;
    }

    public Task<int> CountAsync() => _context.Products.CountAsync();

    public Task<int> CountActiveAsync() => _context.Products.CountAsync(p => p.IsActive);

    public Task<int> CountLowStockAsync(int threshold = 5) =>
        _context.Products.CountAsync(p => p.Stock < threshold);

    private async Task AttachRatingSummariesAsync(IReadOnlyList<Product> products)
    {
        if (products.Count == 0)
        {
            return;
        }

        var productIds = products.Select(product => product.Id).ToList();
        var summaries = await _context.Reviews
            .AsNoTracking()
            .Where(review => productIds.Contains(review.ProductId))
            .GroupBy(review => review.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                ReviewCount = group.Count(),
                AverageRating = group.Average(review => (double)review.Rating)
            })
            .ToDictionaryAsync(summary => summary.ProductId);

        foreach (var product in products)
        {
            if (summaries.TryGetValue(product.Id, out var summary))
            {
                product.ReviewCount = summary.ReviewCount;
                product.AverageRating = summary.AverageRating;
            }
        }
    }
}
