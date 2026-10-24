using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Services;
using OnlineStore.ViewModels;

namespace OnlineStore.Controllers;

public class HomeController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;

    public HomeController(
        IProductService productService,
        ICategoryService categoryService)
    {
        _productService = productService;
        _categoryService = categoryService;
    }

    public async Task<IActionResult> Index()
    {
        // 首页展示最新上架的商品
        var vm = await _productService.GetPagedAsync(new ProductQuery
        {
            PageSize = 8,
            SortBy = "newest"
        });
        vm.SaleItems = await _productService.GetSaleItemsAsync(8);
        vm.Categories = await _categoryService.GetAllAsync();
        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? code = null)
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = code
        });
    }
}
