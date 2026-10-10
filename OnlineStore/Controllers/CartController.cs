using Microsoft.AspNetCore.Mvc;
using OnlineStore.Services;

namespace OnlineStore.Controllers;

/// <summary>购物车：查看、加入、更新数量、移除、清空（基于会话，无需登录）。</summary>
public class CartController : Controller
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<IActionResult> Index()
    {
        var cart = await _cartService.GetCartAsync();
        return View(cart);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1, string? returnUrl = null)
    {
        await _cartService.AddAsync(productId, quantity);
        TempData["Success"] = "已加入购物车。";
        return RedirectToLocal(returnUrl) ?? RedirectToAction("Index", "Products");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int productId, int quantity)
    {
        await _cartService.UpdateAsync(productId, quantity);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int productId)
    {
        await _cartService.RemoveAsync(productId);
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
