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

    public async Task<Cart> RefreshAsync()
    {
        var cart = await GetCartAsync();
        foreach (var item in cart.Items.ToArray())
        {
            await UpdateAsync(item.ProductId, item.SkuId, item.Quantity);
        }
        return await GetCartAsync();
    }

    public async Task<bool> AddAsync(int productId, int quantity = 1, int? skuId = null)
    {

        var product = await _productService.GetByIdAsync(productId);
        if (product is null || !product.IsActive || quantity <= 0)
        {
            return false;
        }

        ProductSku? sku = null;
        if (skuId.HasValue)
        {
            sku = await _skuService.GetByIdAsync(skuId.Value);
            if (sku is null || sku.ProductId != productId || !sku.IsActive)
            {
                return false;
            }
        }

        var price = sku?.Price ?? product.Price;
        var stock = sku is null ? product.Stock : Math.Min(product.Stock, sku.Stock);
        if (stock <= 0)
        {
            return false;
        }

        var items = await _store.GetAsync();
        var existing = items.FirstOrDefault(i => i.ProductId == productId && i.SkuId == skuId);

        if (existing is not null)
        {
            existing.Quantity = (int)Math.Min((long)Math.Max(0, existing.Quantity) + quantity, stock);
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
                NameEn = product.NameEn,
                Color = sku?.Color,
                ColorEn = sku?.ColorEn,
                Spec = sku?.Spec,
                SpecEn = sku?.SpecEn,
                Price = price,
                Quantity = Math.Min(quantity, stock),
                ImageUrl = sku?.ImageUrl ?? product.ImageUrl,
                Stock = stock
            });
        }

        await _store.SaveAsync(items);
        return true;
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
            var product = await _productService.GetByIdAsync(productId);
            var sku = skuId.HasValue ? await _skuService.GetByIdAsync(skuId.Value) : null;
            if (product is null || !product.IsActive || product.Stock <= 0
                || (skuId.HasValue && (sku is null || !sku.IsActive || sku.ProductId != productId || sku.Stock <= 0)))
            {
                items.Remove(item);
            }
            else
            {
                item.Stock = sku is null ? product.Stock : Math.Min(product.Stock, sku.Stock);
                item.Price = sku?.Price ?? product.Price;
                item.Quantity = Math.Min(quantity, item.Stock);
            }
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
