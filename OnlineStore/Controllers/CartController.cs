using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Services;

namespace OnlineStore.Controllers;

/// <summary>购物车：查看、加入、更新数量、移除、清空（基于会话，无需登录）。</summary>
public class CartController : Controller
{
    private readonly ICartService _cartService;
    private readonly IStringLocalizer<SharedResource> _loc;

    public CartController(ICartService cartService, IStringLocalizer<SharedResource> localizer)
    {
        _cartService = cartService;
        _loc = localizer;
    }

    public async Task<IActionResult> Index()
    {
        var cart = await _cartService.GetCartAsync();
        return View(cart);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1, int? skuId = null, string? returnUrl = null, bool buyNow = false)
    {
        await _cartService.AddAsync(productId, quantity, skuId);

        if (buyNow)
        {
            return RedirectToAction("Checkout", "Orders");
        }

        TempData["Success"] = _loc["AddedToCart"].Value;
        return RedirectToLocal(returnUrl) ?? RedirectToAction("Index", "Products");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int productId, int? skuId, int? quantity)
    {
        if (!quantity.HasValue || quantity.Value < 1)
        {
            TempData["Error"] = _loc["CartQuantityRequired"].Value;
            return RedirectToAction(nameof(Index));
        }

        await _cartService.UpdateAsync(productId, skuId, quantity.Value);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int productId, int? skuId)
    {
        await _cartService.RemoveAsync(productId, skuId);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear()
    {
        await _cartService.ClearAsync();
        return RedirectToAction(nameof(Index));
    }

    /// <summary>仅当 returnUrl 为站内地址时回跳，避免开放重定向（安全性）。</summary>
    private IActionResult? RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return null;
    }
}
