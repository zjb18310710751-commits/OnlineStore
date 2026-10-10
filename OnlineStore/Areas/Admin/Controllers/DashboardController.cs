using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Services;

namespace OnlineStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;

    public DashboardController(IProductService productService, ICategoryService categoryService)
    {
        _productService = productService;
        _categoryService = categoryService;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.ProductCount = await _productService.CountAsync();
        ViewBag.ActiveProductCount = await _productService.CountActiveAsync();
        ViewBag.LowStockCount = await _productService.CountLowStockAsync();
        ViewBag.CategoryCount = await _categoryService.CountAsync();
        return View();
    }
}
