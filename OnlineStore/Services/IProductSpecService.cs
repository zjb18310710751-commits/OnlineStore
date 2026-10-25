using OnlineStore.Models;

namespace OnlineStore.Services;

public interface IProductSpecService
{
    Task<IReadOnlyList<ProductSpec>> GetByProductIdAsync(int productId);

    Task<ProductSpec?> GetByIdAsync(int id);

    Task<ProductSpec> CreateAsync(ProductSpec spec);

    Task UpdateAsync(ProductSpec spec);

    Task DeleteAsync(int id);
}
