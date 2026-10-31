using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using OnlineStore.Models;
using OnlineStore.ViewModels;

namespace OnlineStore.Services;

public sealed class StorefrontHeaderService : IStorefrontHeaderService
{
    private readonly ICategoryService _categoryService;
    private readonly IFavoriteService _favoriteService;
    private readonly IOrderService _orderService;
    private readonly ICouponService _couponService;
    private readonly UserManager<ApplicationUser> _userManager;

    public StorefrontHeaderService(
        ICategoryService categoryService,
        IFavoriteService favoriteService,
        IOrderService orderService,
        ICouponService couponService,
        UserManager<ApplicationUser> userManager)
    {
        _categoryService = categoryService;
        _favoriteService = favoriteService;
        _orderService = orderService;
        _couponService = couponService;
        _userManager = userManager;
    }

    public async Task<StorefrontHeaderViewModel> GetAsync(ClaimsPrincipal principal)
    {
        var categories = await _categoryService.GetAllAsync();
        var currentUser = principal.Identity?.IsAuthenticated == true
            ? await _userManager.GetUserAsync(principal)
            : null;

        if (currentUser is null)
        {
            return new StorefrontHeaderViewModel { Categories = categories };
        }

        var favoriteCount = await _favoriteService.CountAsync(currentUser.Id);
        var activeItemCount = await _orderService.GetActiveItemCountAsync(currentUser.Id);
        var availableCouponCount = (await _couponService.GetCustomerCouponsAsync(currentUser.Id))
            .Count(item => item.Status == CouponWalletStatus.Available);

        return new StorefrontHeaderViewModel
        {
            Categories = categories,
            CurrentUser = currentUser,
            FavoriteCount = favoriteCount,
            ActiveItemCount = activeItemCount,
            AvailableCouponCount = availableCouponCount
        };
    }
}
