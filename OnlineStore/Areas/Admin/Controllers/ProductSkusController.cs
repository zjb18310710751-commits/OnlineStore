using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductSkusController : Controller
{
    private readonly IProductSkuService _skuService;
    private readonly IProductService _productService;
    private readonly IStringLocalizer<SharedResource> _loc;

    public ProductSkusController(
        IProductSkuService skuService,
        IProductService productService,
        IStringLocalizer<SharedResource> localizer)
    {
        _skuService = skuService;
        _productService = productService;
        _loc = localizer;
    }

    public async Task<IActionResult> Index(int productId)
    {
        var product = await _productService.GetByIdAsync(productId);
        if (product is null)
        {
            return NotFound();
        }

        ViewBag.Product = product;
        var skus = await _skuService.GetByProductIdAsync(productId);
        return View(skus);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int productId)
    {
        var product = await _productService.GetByIdAsync(productId);
        if (product is null)
        {
            return NotFound();
        }

        ViewBag.Product = product;
        return View(new ProductSku { ProductId = productId, IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductSku sku)
    {
        if (!ModelState.IsValid)
        {
            await LoadProductAsync(sku.ProductId);
            return View(sku);
        }

        await _skuService.CreateAsync(sku);
        TempData["Success"] = _loc["SkuAdded"].Value;
        return RedirectToAction(nameof(Index), new { productId = sku.ProductId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var sku = await _skuService.GetByIdAsync(id);
        if (sku is null)
        {
            return NotFound();
        }

        await LoadProductAsync(sku.ProductId);
        return View(sku);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductSku sku)
    {
        if (id != sku.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await LoadProductAsync(sku.ProductId);
            return View(sku);
        }

        await _skuService.UpdateAsync(sku);
        TempData["Success"] = _loc["SkuUpdated"].Value;
        return RedirectToAction(nameof(Index), new { productId = sku.ProductId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var sku = await _skuService.GetByIdAsync(id);
        var productId = sku?.ProductId ?? 0;
        if (sku is not null)
        {
            await _skuService.DeleteAsync(id);
            TempData["Success"] = _loc["SkuDeleted"].Value;
        }

        return RedirectToAction(nameof(Index), new { productId });
    }

    private async Task LoadProductAsync(int productId)
    {
        ViewBag.Product = await _productService.GetByIdAsync(productId);
    }
}
