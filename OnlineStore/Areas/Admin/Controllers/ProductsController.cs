using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
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
    private readonly IStringLocalizer<SharedResource> _loc;

    public ProductsController(
        IProductService productService,
        ICategoryService categoryService,
        IShopService shopService,
        IImageStorageService imageStorage,
        IStringLocalizer<SharedResource> localizer)
    {
        _productService = productService;
        _categoryService = categoryService;
        _shopService = shopService;
        _imageStorage = imageStorage;
        _loc = localizer;
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
            NameEn = vm.NameEn,
            Slug = SlugHelper.ToSlug(vm.Name),
            Description = vm.Description,
            DescriptionEn = vm.DescriptionEn,
            Price = vm.Price,
            OriginalPrice = vm.OriginalPrice,
            Stock = vm.Stock,
            Sku = vm.Sku,
            ImageUrl = vm.ImageUrl,
            IsActive = vm.IsActive,
            ShopId = vm.ShopId,
            ShippingFrom = vm.ShippingFrom,
            ShippingFromEn = vm.ShippingFromEn,
            ShippingFee = vm.ShippingFee,
            ShippingFeeEn = vm.ShippingFeeEn,
            ShippingFeeAmount = vm.ShippingFeeAmount,
            DeliveryTime = vm.DeliveryTime,
            DeliveryTimeEn = vm.DeliveryTimeEn,
            DetailHtml = vm.DetailHtml
        };

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
        TempData["Success"] = _loc["ProductCreated"].Value;
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
            NameEn = product.NameEn,
            Description = product.Description,
            DescriptionEn = product.DescriptionEn,
            Price = product.Price,
            OriginalPrice = product.OriginalPrice,
            Stock = product.Stock,
            Sku = product.Sku,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive,
            ShopId = product.ShopId,
            CategoryIds = categories.Select(c => c.Id).ToList(),
            ShippingFrom = product.ShippingFrom,
            ShippingFromEn = product.ShippingFromEn,
            ShippingFee = product.ShippingFee,
            ShippingFeeEn = product.ShippingFeeEn,
            ShippingFeeAmount = product.ShippingFeeAmount,
            DeliveryTime = product.DeliveryTime,
            DeliveryTimeEn = product.DeliveryTimeEn,
            DetailHtml = product.DetailHtml
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
        product.NameEn = vm.NameEn;
        product.Slug = SlugHelper.ToSlug(vm.Name);
        product.Description = vm.Description;
        product.DescriptionEn = vm.DescriptionEn;
        product.Price = vm.Price;
        product.OriginalPrice = vm.OriginalPrice;
        product.Stock = vm.Stock;
        product.Sku = vm.Sku;
        product.ImageUrl = vm.ImageUrl;
        product.IsActive = vm.IsActive;
        product.ShopId = vm.ShopId;
        product.ShippingFrom = vm.ShippingFrom;
        product.ShippingFromEn = vm.ShippingFromEn;
        product.ShippingFee = vm.ShippingFee;
        product.ShippingFeeEn = vm.ShippingFeeEn;
        product.ShippingFeeAmount = vm.ShippingFeeAmount;
        product.DeliveryTime = vm.DeliveryTime;
        product.DeliveryTimeEn = vm.DeliveryTimeEn;
        product.DetailHtml = vm.DetailHtml;

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
        await _productService.UpdateAsync(product);
        TempData["Success"] = _loc["ProductUpdated"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _productService.DeleteAsync(id);
        TempData["Success"] = _loc["ProductDeleted"].Value;
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
