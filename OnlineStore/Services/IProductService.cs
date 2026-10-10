using OnlineStore.Models;
using OnlineStore.ViewModels;

namespace OnlineStore.Services;

/// <summary>商品业务抽象。通过接口隔离，便于替换实现与单元测试（可修改性）。</summary>
public interface IProductService
{
    /// <summary>公开目录：分页 + 分类/关键词/价格筛选 + 排序。</summary>
    Task<ProductListViewModel> GetPagedAsync(ProductQuery query);

    Task<Product?> GetByIdAsync(int id);

    Task<Product?> GetByIdWithCategoryAsync(int id);

    /// <summary>后台列表：包含下架商品，支持搜索与分类筛选。</summary>
    Task<(IReadOnlyList<Product> Items, int Total)> GetAdminPagedAsync(
        string? search, int? categoryId, int page, int pageSize);

    Task<Product> CreateAsync(Product product);

    Task UpdateAsync(Product product);

    Task DeleteAsync(int id);

    Task<int> CountAsync();

    Task<int> CountActiveAsync();

    Task<int> CountLowStockAsync(int threshold = 5);
}
