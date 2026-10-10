using OnlineStore.Models;

namespace OnlineStore.Services;

/// <summary>分类业务抽象。</summary>
public interface ICategoryService
{
    Task<IReadOnlyList<Category>> GetAllAsync();

    Task<Category?> GetByIdAsync(int id);

    Task<Category> CreateAsync(Category category);

    Task UpdateAsync(Category category);

    /// <summary>删除分类；分类下仍有商品时返回 false。</summary>
    Task<bool> DeleteAsync(int id);

    Task<int> CountAsync();
}
