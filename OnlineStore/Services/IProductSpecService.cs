using OnlineStore.Models;

namespace OnlineStore.Services;

/// <summary>商品参数业务抽象。</summary>
public interface IProductSpecService
{
    Task<IReadOnlyList<ProductSpec>> GetByProductIdAsync(int productId);

    Task<ProductSpec?> GetByIdAsync(int id);

    Task<ProductSpec> CreateAsync(ProductSpec spec);

    Task UpdateAsync(ProductSpec spec);

    Task DeleteAsync(int id);
}
