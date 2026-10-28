using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ShopsController : Controller
{
    private readonly IShopService _shopService;
    private readonly IStringLocalizer<SharedResource> _loc;

    public ShopsController(IShopService shopService, IStringLocalizer<SharedResource> localizer)
    {
        _shopService = shopService;
        _loc = localizer;
    }

    public async Task<IActionResult> Index(int page = 1) =>
        View(await _shopService.GetPagedAsync(page, 10));

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
        TempData["Success"] = _loc["ShopCreated"].Value;
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
        TempData["Success"] = _loc["ShopUpdated"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _shopService.DeleteAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? _loc["ShopDeleted"].Value : _loc["ShopDeleteFailed"].Value;
        return RedirectToAction(nameof(Index));
    }
}
