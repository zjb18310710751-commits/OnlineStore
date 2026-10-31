using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.ViewModels;

namespace OnlineStore.Services;

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _context;

    public CategoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync() =>
        await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

    public async Task<IReadOnlyList<CategoryNavigationCategory>> GetNavigationAsync(CancellationToken cancellationToken = default) =>
        await _context.Categories
            .AsNoTracking()
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .Select(category => new CategoryNavigationCategory
            {
                Id = category.Id,
                Name = category.Name,
                NameEn = category.NameEn,
                Products = category.Products
                    .Where(product => product.IsActive)
                    .OrderByDescending(product => product.CreatedAt)
                    .ThenByDescending(product => product.Id)
                    .Take(12)
                    .Select(product => new CategoryNavigationProduct
                    {
                        Id = product.Id,
                        Name = product.Name,
                        NameEn = product.NameEn
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<Category>> GetPagedAsync(int page, int pageSize)
    {
        var query = _context.Categories.AsNoTracking()
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name);
        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<Category>(items, page, pageSize, total);
    }

    public Task<Category?> GetByIdAsync(int id) =>
        _context.Categories.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Category> CreateAsync(Category category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task UpdateAsync(Category category)
    {
        _context.Categories.Update(category);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
        {
            return false;
        }

        if (category.Products.Any())
        {
            return false;
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return true;
    }

}
