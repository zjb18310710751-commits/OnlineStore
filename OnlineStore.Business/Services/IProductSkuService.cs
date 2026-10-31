using OnlineStore.Models;

namespace OnlineStore.Services;

public interface IProductSkuService
{
    Task<IReadOnlyList<ProductSku>> GetByProductIdAsync(int productId);

    Task<ProductSku?> GetByIdAsync(int id);

    Task<ProductSku> CreateAsync(ProductSku sku);

    Task UpdateAsync(ProductSku sku);

    Task DeleteAsync(int id);
}
