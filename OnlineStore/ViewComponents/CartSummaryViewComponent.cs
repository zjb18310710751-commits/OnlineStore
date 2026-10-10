using Microsoft.AspNetCore.Mvc;
using OnlineStore.Services;

namespace OnlineStore.ViewComponents;

/// <summary>导航栏购物车徽标：显示当前购物车内的商品总件数。</summary>
public class CartSummaryViewComponent : ViewComponent
{
    private readonly ICartService _cartService;

    public CartSummaryViewComponent(ICartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var cart = await _cartService.GetCartAsync();
        return View(cart.TotalItems);
    }
}
