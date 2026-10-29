using OnlineStore.Models;
using OnlineStore.ViewModels;

namespace OnlineStore.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<Category>> GetAllAsync();
    Task<IReadOnlyList<CategoryNavigationCategory>> GetNavigationAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<Category>> GetPagedAsync(int page, int pageSize);

    Task<Category?> GetByIdAsync(int id);

    Task<Category> CreateAsync(Category category);

    Task UpdateAsync(Category category);

    Task<bool> DeleteAsync(int id);

}
