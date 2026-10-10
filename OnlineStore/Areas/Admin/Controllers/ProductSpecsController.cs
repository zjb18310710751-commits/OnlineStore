using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductSpecsController : Controller
{
    private readonly IProductSpecService _specService;
    private readonly IProductService _productService;

    public ProductSpecsController(IProductSpecService specService, IProductService productService)
    {
        _specService = specService;
        _productService = productService;
    }

    public async Task<IActionResult> Index(int productId)
    {
        var product = await _productService.GetByIdAsync(productId);
        if (product is null)
        {
            return NotFound();
        }

        ViewBag.Product = product;
        var specs = await _specService.GetByProductIdAsync(productId);
        return View(specs);
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
        return View(new ProductSpec { ProductId = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductSpec spec)
    {
        if (!ModelState.IsValid)
        {
            await LoadProductAsync(spec.ProductId);
            return View(spec);
        }

        await _specService.CreateAsync(spec);
        TempData["Success"] = "参数已添加。";
        return RedirectToAction(nameof(Index), new { productId = spec.ProductId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var spec = await _specService.GetByIdAsync(id);
        if (spec is null)
        {
            return NotFound();
        }

        await LoadProductAsync(spec.ProductId);
        return View(spec);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductSpec spec)
    {
        if (id != spec.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await LoadProductAsync(spec.ProductId);
            return View(spec);
        }

        await _specService.UpdateAsync(spec);
        TempData["Success"] = "参数已更新。";
        return RedirectToAction(nameof(Index), new { productId = spec.ProductId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var spec = await _specService.GetByIdAsync(id);
        var productId = spec?.ProductId ?? 0;
        if (spec is not null)
        {
            await _specService.DeleteAsync(id);
            TempData["Success"] = "参数已删除。";
        }

        return RedirectToAction(nameof(Index), new { productId });
    }

    private async Task LoadProductAsync(int productId)
    {
        ViewBag.Product = await _productService.GetByIdAsync(productId);
    }
}
