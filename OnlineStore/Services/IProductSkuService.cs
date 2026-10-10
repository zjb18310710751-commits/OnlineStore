using OnlineStore.Models;

namespace OnlineStore.Services;

/// <summary>商品规格变体（SKU）业务抽象。</summary>
public interface IProductSkuService
{
    Task<IReadOnlyList<ProductSku>> GetByProductIdAsync(int productId);

    Task<ProductSku?> GetByIdAsync(int id);

    Task<ProductSku> CreateAsync(ProductSku sku);

    Task UpdateAsync(ProductSku sku);

    Task DeleteAsync(int id);
}
