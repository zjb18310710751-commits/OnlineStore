using OnlineStore.Models;

namespace OnlineStore.Services;

public interface IHeroSlideService
{
    Task<IReadOnlyList<HeroSlide>> GetAllAsync(bool activeOnly = false);
    Task<HeroSlide?> GetByIdAsync(int id);
    Task<HeroSlide> CreateAsync(HeroSlide slide);
    Task UpdateAsync(HeroSlide slide);
    Task<bool> DeleteAsync(int id);
}
