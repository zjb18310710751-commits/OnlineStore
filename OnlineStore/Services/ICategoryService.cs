using OnlineStore.Models;

namespace OnlineStore.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<Category>> GetAllAsync();

    Task<Category?> GetByIdAsync(int id);

    Task<Category> CreateAsync(Category category);

    Task UpdateAsync(Category category);

    Task<bool> DeleteAsync(int id);

    Task<int> CountAsync();
}
