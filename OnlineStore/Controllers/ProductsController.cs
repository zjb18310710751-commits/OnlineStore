using Microsoft.AspNetCore.Mvc;
using OnlineStore.Services;
using OnlineStore.ViewModels;

namespace OnlineStore.Controllers;

/// <summary>公开商品浏览、筛选与详情。</summary>
public class ProductsController : Controller
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<IActionResult> Index(ProductQuery query)
    {
        var vm = await _productService.GetPagedAsync(query);
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _productService.GetByIdWithCategoryAsync(id);
        if (product is null || !product.IsActive)
        {
            return NotFound();
        }

        return View(product);
    }
}
