using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Controllers;

[Authorize]
public class CouponsController : Controller
{
    private readonly ICouponService _couponService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<SharedResource> _loc;

    public CouponsController(
        ICouponService couponService,
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<SharedResource> localizer)
    {
        _couponService = couponService;
        _userManager = userManager;
        _loc = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var coupons = await _couponService.GetCustomerCouponsAsync(user.Id);
        return View(coupons);
    }
}
