using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.Models;

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
