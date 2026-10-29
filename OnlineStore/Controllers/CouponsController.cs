using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Controllers;

[Authorize]
public class CouponsController : Controller
{
    private readonly ICouponService _couponService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CouponsController(
        ICouponService couponService,
        UserManager<ApplicationUser> userManager)
    {
        _couponService = couponService;
        _userManager = userManager;
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
