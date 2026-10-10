using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.ViewModels;

namespace OnlineStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductsController : Controller
{
    private const int PageSize = 10;

    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;

    public ProductsController(IProductService productService, ICategoryService categoryService)
    {
        _productService = productService;
        _categoryService = categoryService;
    }

    public async Task<IActionResult> Index(string? search, int? categoryId, int page = 1)
    {
        var (items, total) = await _productService.GetAdminPagedAsync(search, categoryId, page, PageSize);

        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.Page = page;
        ViewBag.PageSize = PageSize;
        ViewBag.Total = total;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        ViewBag.Categories = await _categoryService.GetAllAsync();

        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new ProductFormViewModel { IsActive = true };
        await PopulateCategoriesAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(vm);
            return View(vm);
        }

        var product = new Product
        {
            Name = vm.Name,
            Slug = SlugHelper.ToSlug(vm.Name),
            Description = vm.Description,
            Price = vm.Price,
            Stock = vm.Stock,
            Sku = vm.Sku,
            ImageUrl = vm.ImageUrl,
            IsActive = vm.IsActive,
            CategoryId = vm.CategoryId
        };

        await _productService.CreateAsync(product);
        TempData["Success"] = "商品已创建。";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        var vm = new ProductFormViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            Sku = product.Sku,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive,
            CategoryId = product.CategoryId
        };

        await PopulateCategoriesAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductFormViewModel vm)
    {
        if (id != vm.Id)
        {
            return NotFound();
        }

        var product = await _productService.GetByIdAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(vm);
            return View(vm);
        }

        product.Name = vm.Name;
        product.Slug = SlugHelper.ToSlug(vm.Name);
        product.Description = vm.Description;
        product.Price = vm.Price;
        product.Stock = vm.Stock;
        product.Sku = vm.Sku;
        product.ImageUrl = vm.ImageUrl;
        product.IsActive = vm.IsActive;
        product.CategoryId = vm.CategoryId;

        await _productService.UpdateAsync(product);
        TempData["Success"] = "商品已更新。";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _productService.DeleteAsync(id);
        TempData["Success"] = "商品已删除。";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _productService.GetByIdWithCategoryAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        return View(product);
    }

    private async Task PopulateCategoriesAsync(ProductFormViewModel vm)
    {
        var categories = await _categoryService.GetAllAsync();
        vm.Categories = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString()));
    }
}
