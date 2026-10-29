using OnlineStore.Models;

namespace OnlineStore.ViewModels;

public class ShopViewModel
{
    public Shop Shop { get; set; } = null!;

    public IReadOnlyList<Product> Products { get; set; } = new List<Product>();
}
