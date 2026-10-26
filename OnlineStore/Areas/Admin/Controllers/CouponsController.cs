using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using OnlineStore.Data;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.ViewModels;

namespace OnlineStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CouponsController : Controller
{
    private readonly ICouponService _couponService;
    private readonly ApplicationDbContext _context;
    private readonly IStringLocalizer<SharedResource> _loc;

    public CouponsController(
        ICouponService couponService,
        ApplicationDbContext context,
        IStringLocalizer<SharedResource> localizer)
    {
        _couponService = couponService;
        _context = context;
        _loc = localizer;
    }

    public async Task<IActionResult> Index() =>
        View(await _couponService.GetAllAsync());

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CouponFormViewModel
        {
            IsActive = true,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddMonths(1)
        };
        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CouponFormViewModel form)
    {
        form.Code = await _couponService.GenerateUniqueCodeAsync(form.CampaignName);
        ModelState.Remove(nameof(form.Code));
        ValidateForm(form);
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(form);
            return View(form);
        }

        try
        {
            await _couponService.CreateAsync(ToCoupon(form), form.CategoryIds, form.ProductIds);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(form.Code), _loc["CouponCodeExists"].Value);
            await PopulateOptionsAsync(form);
            return View(form);
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

        var model = ToFormModel(coupon);
        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CouponFormViewModel form)
    {
        if (id != form.Id)
        {
            return NotFound();
        }

        ValidateForm(form);
        var existing = await _couponService.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        form.Code = existing.Code;
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(form);
            return View(form);
        }

        ApplyForm(existing, form);
        try
        {
            await _couponService.UpdateAsync(existing, form.CategoryIds, form.ProductIds);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(form.Code), _loc["CouponCodeExists"].Value);
            await PopulateOptionsAsync(form);
            return View(form);
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateBatch(CouponBatchViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = _loc["CouponBatchFailed"].Value;
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var count = await _couponService.GenerateBatchAsync(model.CouponId, model.Quantity, null);
            TempData["Success"] = _loc["CouponBatchCreated", count].Value;
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Export()
    {
        var coupons = await _couponService.GetAllAsync();
        var builder = new StringBuilder();
        builder.AppendLine("Name,Type,Value,MaxDiscount,MinOrder,Start,End,UsageLimit,PerUserLimit,UsedCount,Scope,FirstOrderOnly,ExcludeDiscountedItems,Active");
        foreach (var coupon in coupons)
        {
            builder.AppendLine(string.Join(',',
                Csv(coupon.CampaignName),
                Csv(coupon.DiscountType.ToString()),
                coupon.DiscountValue.ToString("0.##"),
                coupon.MaxDiscountAmount?.ToString("0.##"),
                coupon.MinOrderAmount?.ToString("0.##"),
                coupon.StartDate.ToString("yyyy-MM-dd"),
                coupon.EndDate.ToString("yyyy-MM-dd"),
                coupon.UsageLimit?.ToString(),
                coupon.PerUserLimit?.ToString(),
                coupon.UsedCount,
                Csv(coupon.Scope.ToString()),
                coupon.FirstOrderOnly,
                coupon.ExcludeDiscountedItems,
                coupon.IsActive));
        }

        return File(
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(builder.ToString()),
            "text/csv; charset=utf-8",
            $"coupons-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    private async Task PopulateOptionsAsync(CouponFormViewModel form)
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .ToListAsync();
        var products = await _context.Products
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .ToListAsync();

        form.CategoryOptions = categories
            .Select(category => new SelectListItem(category.Name, category.Id.ToString()))
            .ToList();
        form.ProductOptions = products
            .Select(product => new SelectListItem(
                $"{product.Name} · {product.Sku}".TrimEnd(' ', '·'),
                product.Id.ToString()))
            .ToList();
    }

    private void ValidateForm(CouponFormViewModel form)
    {
        if (form.DiscountType == CouponDiscountType.Percent && form.DiscountValue > 100)
        {
            ModelState.AddModelError(nameof(form.DiscountValue), _loc["CouponPercentRange"].Value);
        }
        if (form.DiscountType != CouponDiscountType.FreeShipping && form.DiscountValue <= 0)
        {
            ModelState.AddModelError(nameof(form.DiscountValue), _loc["CouponValueInvalid"].Value);
        }
        if (form.EndDate.Date < form.StartDate.Date)
        {
            ModelState.AddModelError(nameof(form.EndDate), _loc["CouponDatesInvalid"].Value);
        }
        if (form.Scope == CouponScope.Categories && form.CategoryIds.Count == 0)
        {
            ModelState.AddModelError(nameof(form.CategoryIds), _loc["CouponCategoryRequired"].Value);
        }
        if (form.Scope == CouponScope.Products && form.ProductIds.Count == 0)
        {
            ModelState.AddModelError(nameof(form.ProductIds), _loc["CouponProductRequired"].Value);
        }
    }

    private static CouponFormViewModel ToFormModel(Coupon coupon) => new()
    {
        Id = coupon.Id,
        Code = coupon.Code,
        CampaignName = coupon.CampaignName ?? string.Empty,
        DiscountType = coupon.DiscountType,
        DiscountValue = coupon.DiscountValue,
        MaxDiscountAmount = coupon.MaxDiscountAmount,
        MinOrderAmount = coupon.MinOrderAmount,
        StartDate = coupon.StartDate.Date,
        EndDate = coupon.EndDate.Date,
        IsActive = coupon.IsActive,
        UsageLimit = coupon.UsageLimit,
        PerUserLimit = coupon.PerUserLimit,
        FirstOrderOnly = coupon.FirstOrderOnly,
        ExcludeDiscountedItems = coupon.ExcludeDiscountedItems,
        Scope = coupon.Scope,
        CategoryIds = coupon.Categories.Select(category => category.Id).ToList(),
        ProductIds = coupon.Products.Select(product => product.Id).ToList()
    };

    private static Coupon ToCoupon(CouponFormViewModel form)
    {
        var coupon = new Coupon();
        ApplyForm(coupon, form);
        return coupon;
    }

    private static void ApplyForm(Coupon coupon, CouponFormViewModel form)
    {
        coupon.Code = form.Code;
        coupon.CampaignName = form.CampaignName;
        coupon.DiscountType = form.DiscountType;
        coupon.DiscountValue = form.DiscountType == CouponDiscountType.FreeShipping ? 0 : form.DiscountValue;
        coupon.MaxDiscountAmount = form.MaxDiscountAmount;
        coupon.MinOrderAmount = form.MinOrderAmount;
        coupon.StartDate = DateTime.SpecifyKind(form.StartDate.Date, DateTimeKind.Utc);
        coupon.EndDate = DateTime.SpecifyKind(form.EndDate.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
        coupon.IsActive = form.IsActive;
        coupon.UsageLimit = form.UsageLimit;
        coupon.PerUserLimit = form.PerUserLimit;
        coupon.FirstOrderOnly = form.FirstOrderOnly;
        coupon.ExcludeDiscountedItems = form.ExcludeDiscountedItems;
        coupon.Scope = form.Scope;
    }

    private static string Csv(string? value) =>
        $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
}
