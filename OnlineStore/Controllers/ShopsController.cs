using Microsoft.AspNetCore.Mvc;
using OnlineStore.Services;
using OnlineStore.ViewModels;

namespace OnlineStore.Controllers;

public class ShopsController : Controller
{
    private readonly IShopService _shopService;
    private readonly IProductService _productService;

    public ShopsController(IShopService shopService, IProductService productService)
    {
        _shopService = shopService;
        _productService = productService;
    }

    public async Task<IActionResult> Index(int id)
    {
        var shop = await _shopService.GetByIdAsync(id);
        if (shop is null)
        {
            return NotFound();
        }

        var products = await _productService.GetByShopAsync(id);
        return View(new ShopViewModel { Shop = shop, Products = products });
    }
}
