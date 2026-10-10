using OnlineStore.Models;

namespace OnlineStore.Services;

public class CartService : ICartService
{
    private readonly ICartStore _store;
    private readonly IProductService _productService;

    public CartService(ICartStore store, IProductService productService)
    {
        _store = store;
        _productService = productService;
    }

    public async Task<Cart> GetCartAsync()
    {
        var items = await _store.GetAsync();
        return new Cart { Items = items };
    }

    public async Task AddAsync(int productId, int quantity = 1)
    {
        // 只允许上架且库存大于 0 的商品加入（安全性/业务校验）
        var product = await _productService.GetByIdAsync(productId);
        if (product is null || !product.IsActive || product.Stock <= 0)
        {
            return;
        }

        quantity = Math.Max(1, quantity);
        var items = await _store.GetAsync();
        var existing = items.FirstOrDefault(i => i.ProductId == productId);

        if (existing is not null)
        {
            existing.Quantity = Math.Min(existing.Quantity + quantity, product.Stock);
            existing.Price = product.Price; // 以最新价格为准
            existing.Stock = product.Stock;
        }
        else
        {
            items.Add(new CartItem
            {
                ProductId = product.Id,
                Name = product.Name,
                Price = product.Price,
                Quantity = Math.Min(quantity, product.Stock),
                ImageUrl = product.ImageUrl,
                Stock = product.Stock
            });
        }

        await _store.SaveAsync(items);
    }

    public async Task UpdateAsync(int productId, int quantity)
    {
        var items = await _store.GetAsync();
        var item = items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null)
        {
            return;
        }

        if (quantity <= 0)
        {
            items.Remove(item);
        }
        else
        {
            var product = await _productService.GetByIdAsync(productId);
            var max = product?.Stock ?? item.Stock;
            item.Quantity = Math.Min(quantity, Math.Max(1, max));
        }

        await _store.SaveAsync(items);
    }

    public async Task RemoveAsync(int productId)
    {
        var items = await _store.GetAsync();
        items.RemoveAll(i => i.ProductId == productId);
        await _store.SaveAsync(items);
    }

    public async Task ClearAsync()
    {
        await _store.SaveAsync(new List<CartItem>());
    }
}
