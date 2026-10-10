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
    private readonly IShopService _shopService;
    private readonly IImageStorageService _imageStorage;

    public ProductsController(
        IProductService productService,
        ICategoryService categoryService,
        IShopService shopService,
        IImageStorageService imageStorage)
    {
        _productService = productService;
        _categoryService = categoryService;
        _shopService = shopService;
        _imageStorage = imageStorage;
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
        await PopulateFormAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateFormAsync(vm);
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
            ShopId = vm.ShopId,
            ShippingFrom = vm.ShippingFrom,
            ShippingFee = vm.ShippingFee,
            DeliveryTime = vm.DeliveryTime
        };

        // 若上传了图片，则覆盖表单中的图片地址
        if (vm.ImageFile is { Length: > 0 })
        {
            try
            {
                product.ImageUrl = await _imageStorage.SaveAsync(vm.ImageFile);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(vm.ImageFile), ex.Message);
                await PopulateFormAsync(vm);
                return View(vm);
            }
        }

        await _productService.SetCategoriesAsync(product, vm.CategoryIds);
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

        var categories = await _productService.GetCategoriesAsync(id);
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
            ShopId = product.ShopId,
            CategoryIds = categories.Select(c => c.Id).ToList(),
            ShippingFrom = product.ShippingFrom,
            ShippingFee = product.ShippingFee,
            DeliveryTime = product.DeliveryTime
        };

        await PopulateFormAsync(vm);
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
            await PopulateFormAsync(vm);
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
        product.ShopId = vm.ShopId;
        product.ShippingFrom = vm.ShippingFrom;
        product.ShippingFee = vm.ShippingFee;
        product.DeliveryTime = vm.DeliveryTime;

        // 若上传了新图片，保存成功后清理被替换的旧本地图片
        if (vm.ImageFile is { Length: > 0 })
        {
            var previousImageUrl = product.ImageUrl;
            try
            {
                product.ImageUrl = await _imageStorage.SaveAsync(vm.ImageFile);
                _imageStorage.Delete(previousImageUrl);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(vm.ImageFile), ex.Message);
                await PopulateFormAsync(vm);
                return View(vm);
            }
        }

        await _productService.SetCategoriesAsync(product, vm.CategoryIds);
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
        var product = await _productService.GetByIdWithDetailsAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        return View(product);
    }

    private async Task PopulateFormAsync(ProductFormViewModel vm)
    {
        var shops = await _shopService.GetAllAsync();
        vm.Shops = shops.Select(s => new SelectListItem(s.Name, s.Id.ToString()));

        var categories = await _categoryService.GetAllAsync();
        vm.Categories = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString()));
    }
}
