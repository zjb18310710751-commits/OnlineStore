using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.ViewModels;

namespace OnlineStore.Services;

public class HeroSlideService : IHeroSlideService
{
    private readonly ApplicationDbContext _context;

    public HeroSlideService(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<HeroSlide>> GetAllAsync(bool activeOnly = false)
    {
        var query = _context.HeroSlides.AsNoTracking();
        if (activeOnly) query = query.Where(slide => slide.IsActive);
        return await query.OrderBy(slide => slide.DisplayOrder).ThenBy(slide => slide.Id).ToListAsync();
    }

    public async Task<PagedResult<HeroSlide>> GetPagedAsync(int page, int pageSize)
    {
        var query = _context.HeroSlides.AsNoTracking()
            .OrderBy(slide => slide.DisplayOrder)
            .ThenBy(slide => slide.Id);
        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<HeroSlide>(items, page, pageSize, total);
    }

    public Task<int> CountActiveAsync() => _context.HeroSlides.CountAsync(slide => slide.IsActive);

    public Task<HeroSlide?> GetByIdAsync(int id) =>
        _context.HeroSlides.FirstOrDefaultAsync(slide => slide.Id == id);

    public async Task<HeroSlide> CreateAsync(HeroSlide slide)
    {
        _context.HeroSlides.Add(slide);
        await _context.SaveChangesAsync();
        return slide;
    }

    public async Task UpdateAsync(HeroSlide slide)
    {
        _context.HeroSlides.Update(slide);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var slide = await _context.HeroSlides.FindAsync(id);
        if (slide is null) return false;
        _context.HeroSlides.Remove(slide);
        await _context.SaveChangesAsync();
        return true;
    }
}
