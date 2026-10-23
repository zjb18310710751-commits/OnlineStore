using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Controllers;

/// <summary>收藏（心愿单）：登录用户可收藏/取消收藏商品，并查看我的收藏。</summary>
[Authorize]
public class FavoritesController : Controller
{
    private readonly IFavoriteService _favoriteService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<SharedResource> _loc;

    public FavoritesController(
        IFavoriteService favoriteService,
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<SharedResource> localizer)
    {
        _favoriteService = favoriteService;
        _userManager = userManager;
        _loc = localizer;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int productId, string? returnUrl = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var nowFavorite = await _favoriteService.ToggleAsync(user.Id, productId);
        TempData["Success"] = nowFavorite ? _loc["FavoriteAdded"].Value : _loc["FavoriteRemoved"].Value;
        return RedirectToLocal(returnUrl) ?? RedirectToAction("Details", "Products", new { id = productId });
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var products = await _favoriteService.GetFavoritesAsync(user.Id);
        return View(products);
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
