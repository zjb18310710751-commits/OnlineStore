using Microsoft.AspNetCore.Mvc;
using OnlineStore.Services;

namespace OnlineStore.ViewComponents;

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
        return View(cart.ItemCount);
    }
}
