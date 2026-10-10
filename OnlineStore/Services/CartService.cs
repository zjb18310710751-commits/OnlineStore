using OnlineStore.Models;

namespace OnlineStore.Services;

public class CartService : ICartService
{
    private readonly ICartStore _store;
    private readonly IProductService _productService;
    private readonly IProductSkuService _skuService;

    public CartService(ICartStore store, IProductService productService, IProductSkuService skuService)
    {
        _store = store;
        _productService = productService;
        _skuService = skuService;
    }

    public async Task<Cart> GetCartAsync()
    {
        var items = await _store.GetAsync();
        return new Cart { Items = items };
    }

    public async Task AddAsync(int productId, int quantity = 1, int? skuId = null)
    {
        // 只允许上架且库存大于 0 的商品加入（安全性/业务校验）
        var product = await _productService.GetByIdAsync(productId);
        if (product is null || !product.IsActive)
        {
            return;
        }

        ProductSku? sku = null;
        if (skuId.HasValue)
        {
            sku = await _skuService.GetByIdAsync(skuId.Value);
            if (sku is null || sku.ProductId != productId || !sku.IsActive)
            {
                sku = null; // 无效的规格回退到商品本身
            }
        }

        var price = sku?.Price ?? product.Price;
        var stock = sku?.Stock ?? product.Stock;
        if (stock <= 0)
        {
            return;
        }

        quantity = Math.Max(1, quantity);
        var items = await _store.GetAsync();
        var existing = items.FirstOrDefault(i => i.ProductId == productId && i.SkuId == skuId);

        if (existing is not null)
        {
            existing.Quantity = Math.Min(existing.Quantity + quantity, stock);
            existing.Price = price;
            existing.Stock = stock;
        }
        else
        {
            items.Add(new CartItem
            {
                ProductId = product.Id,
                SkuId = sku?.Id,
                Name = product.Name,
                Color = sku?.Color,
                Spec = sku?.Spec,
                Price = price,
                Quantity = Math.Min(quantity, stock),
                ImageUrl = sku?.ImageUrl ?? product.ImageUrl,
                Stock = stock
            });
        }

        await _store.SaveAsync(items);
    }

    public async Task UpdateAsync(int productId, int? skuId, int quantity)
    {
        var items = await _store.GetAsync();
        var item = items.FirstOrDefault(i => i.ProductId == productId && i.SkuId == skuId);
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
            item.Quantity = Math.Min(quantity, Math.Max(1, item.Stock));
        }

        await _store.SaveAsync(items);
    }

    public async Task RemoveAsync(int productId, int? skuId)
    {
        var items = await _store.GetAsync();
        items.RemoveAll(i => i.ProductId == productId && i.SkuId == skuId);
        await _store.SaveAsync(items);
    }

    public async Task ClearAsync()
    {
        await _store.SaveAsync(new List<CartItem>());
    }
}
