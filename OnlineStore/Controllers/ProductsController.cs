using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.ViewModels;

namespace OnlineStore.Controllers;

/// <summary>公开商品浏览、筛选与详情。</summary>
public class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly IFavoriteService _favoriteService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProductsController(
        IProductService productService,
        IFavoriteService favoriteService,
        UserManager<ApplicationUser> userManager)
    {
        _productService = productService;
        _favoriteService = favoriteService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(ProductQuery query)
    {
        var vm = await _productService.GetPagedAsync(query);
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _productService.GetByIdWithDetailsAsync(id);
        if (product is null || !product.IsActive)
        {
            return NotFound();
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is not null)
            {
                ViewData["IsFavorite"] = await _favoriteService.IsFavoriteAsync(user.Id, id);
            }
        }

        return View(product);
    }
}
