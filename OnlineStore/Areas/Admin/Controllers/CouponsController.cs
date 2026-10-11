using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CouponsController : Controller
{
    private readonly ICouponService _couponService;

    public CouponsController(ICouponService couponService)
    {
        _couponService = couponService;
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
            ModelState.AddModelError(nameof(Coupon.Code), "优惠码已存在。");
            return View(coupon);
        }

        TempData["Success"] = "优惠券已创建。";
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

        // 只更新可编辑字段，保留已用次数与创建时间
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
            ModelState.AddModelError(nameof(Coupon.Code), "优惠码已存在。");
            return View(coupon);
        }

        TempData["Success"] = "优惠券已更新。";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _couponService.DeleteAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? "优惠券已删除。" : "删除失败：优惠券不存在。";
        return RedirectToAction(nameof(Index));
    }
}
