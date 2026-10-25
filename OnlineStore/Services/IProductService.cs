using OnlineStore.Models;
using OnlineStore.ViewModels;

namespace OnlineStore.Services;

public interface IProductService
{

    Task<ProductListViewModel> GetPagedAsync(ProductQuery query);

    Task<Product?> GetByIdAsync(int id);

    Task<Product?> GetByIdWithDetailsAsync(int id);

    Task<(IReadOnlyList<Product> Items, int Total)> GetAdminPagedAsync(
        string? search, int? categoryId, int page, int pageSize);

    Task<Product> CreateAsync(Product product);

    Task UpdateAsync(Product product);

    Task DeleteAsync(int id);

    Task<IReadOnlyList<Category>> GetCategoriesAsync(int productId);

    Task SetCategoriesAsync(Product product, IEnumerable<int> categoryIds);

    Task<IReadOnlyList<Product>> GetByShopAsync(int shopId);

    Task<int> CountAsync();

    Task<int> CountActiveAsync();

    Task<int> CountLowStockAsync(int threshold = 5);

    Task<IReadOnlyList<Product>> GetSaleItemsAsync(int count = 8);
}
