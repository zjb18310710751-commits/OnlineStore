using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ShopsController : Controller
{
    private readonly IShopService _shopService;

    public ShopsController(IShopService shopService)
    {
        _shopService = shopService;
    }

    public async Task<IActionResult> Index()
    {
        var shops = await _shopService.GetAllAsync();
        return View(shops);
    }

    [HttpGet]
    public IActionResult Create() => View(new Shop());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Shop shop)
    {
        if (!ModelState.IsValid)
        {
            return View(shop);
        }

        await _shopService.CreateAsync(shop);
        TempData["Success"] = "店铺已创建。";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var shop = await _shopService.GetByIdAsync(id);
        if (shop is null)
        {
            return NotFound();
        }

        return View(shop);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Shop shop)
    {
        if (id != shop.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(shop);
        }

        await _shopService.UpdateAsync(shop);
        TempData["Success"] = "店铺已更新。";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _shopService.DeleteAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? "店铺已删除。" : "删除失败：店铺下仍有商品。";
        return RedirectToAction(nameof(Index));
    }
}
