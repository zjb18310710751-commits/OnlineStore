using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Services;

namespace OnlineStore.Controllers;

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
        var cart = await _cartService.RefreshAsync();
        return View(cart);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1, int? skuId = null, string? returnUrl = null, bool buyNow = false)
    {
        var added = await _cartService.AddAsync(productId, quantity, skuId);

        if (!added)
        {
            TempData["Error"] = _loc["CartUnavailable"].Value;
            return RedirectToLocal(returnUrl) ?? RedirectToAction("Index", "Products");
        }

        if (buyNow)
        {
            return RedirectToAction("Checkout", "Orders");
        }

        TempData["Success"] = _loc["CartAdded"].Value;
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

    private IActionResult? RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return null;
    }
}
