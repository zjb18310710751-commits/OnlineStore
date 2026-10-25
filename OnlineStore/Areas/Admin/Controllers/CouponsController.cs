using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CouponsController : Controller
{
    private readonly ICouponService _couponService;
    private readonly IStringLocalizer<SharedResource> _loc;

    public CouponsController(ICouponService couponService, IStringLocalizer<SharedResource> localizer)
    {
        _couponService = couponService;
        _loc = localizer;
    }

    public async Task<IActionResult> Index()
    {
        var coupons = await _couponService.GetAllAsync();
        return View(coupons);
    }

    [HttpGet]
    public IActionResult Create() =>
        View(new Coupon
        {
            IsActive = true,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1)
        });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Coupon coupon)
    {
        if (!ModelState.IsValid)
        {
            return View(coupon);
        }

        try
        {
            await _couponService.CreateAsync(coupon);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(Coupon.Code), _loc["CouponCodeExists"].Value);
            return View(coupon);
        }

        TempData["Success"] = _loc["CouponCreated"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var coupon = await _couponService.GetByIdAsync(id);
        if (coupon is null)
        {
            return NotFound();
        }

        return View(coupon);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Coupon coupon)
    {
        if (id != coupon.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(coupon);
        }

        var existing = await _couponService.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Code = coupon.Code;
        existing.DiscountType = coupon.DiscountType;
        existing.DiscountValue = coupon.DiscountValue;
        existing.MinOrderAmount = coupon.MinOrderAmount;
        existing.StartDate = coupon.StartDate;
        existing.EndDate = coupon.EndDate;
        existing.IsActive = coupon.IsActive;
        existing.UsageLimit = coupon.UsageLimit;

        try
        {
            await _couponService.UpdateAsync(existing);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(Coupon.Code), _loc["CouponCodeExists"].Value);
            return View(coupon);
        }

        TempData["Success"] = _loc["CouponUpdated"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _couponService.DeleteAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? _loc["CouponDeleted"].Value : _loc["CouponDeleteFailed"].Value;
        return RedirectToAction(nameof(Index));
    }
}
