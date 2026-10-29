using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;

namespace OnlineStore.Services;

public class FavoriteService : IFavoriteService
{
    private readonly ApplicationDbContext _context;

    public FavoriteService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<bool> IsFavoriteAsync(string userId, int productId) =>
        _context.Favorites.AnyAsync(f => f.UserId == userId && f.ProductId == productId);

    public async Task<bool> ToggleAsync(string userId, int productId)
    {
        var existing = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

        if (existing is not null)
        {
            _context.Favorites.Remove(existing);
            await _context.SaveChangesAsync();
            return false;
        }

        if (!await _context.Products.AnyAsync(p => p.Id == productId && p.IsActive))
        {
            return false;
        }

        _context.Favorites.Add(new Favorite { UserId = userId, ProductId = productId });
        await _context.SaveChangesAsync();
        return true;
    }

    public Task<int> CountAsync(string userId) =>
        _context.Favorites.CountAsync(f => f.UserId == userId);

    public async Task<IReadOnlyList<Product>> GetFavoritesAsync(string userId)
    {
        var ids = await _context.Favorites
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => f.ProductId)
            .ToListAsync();

        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Categories)
            .Where(p => ids.Contains(p.Id) && p.IsActive)
            .ToListAsync();

        var dict = products.ToDictionary(p => p.Id);
        return ids.Where(dict.ContainsKey).Select(id => dict[id]).ToList();
    }
}
