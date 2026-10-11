using OnlineStore.Models;

namespace OnlineStore.ViewModels;

/// <summary>店铺主页视图模型：店铺信息 + 该店在售商品。</summary>
public class ShopViewModel
{
    public Shop Shop { get; set; } = null!;

    public IReadOnlyList<Product> Products { get; set; } = new List<Product>();
}
