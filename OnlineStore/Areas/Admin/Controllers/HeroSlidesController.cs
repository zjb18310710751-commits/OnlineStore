using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.ViewModels;

namespace OnlineStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class HeroSlidesController : Controller
{
    private readonly IHeroSlideService _slides;
    private readonly IImageStorageService _images;

    public HeroSlidesController(IHeroSlideService slides, IImageStorageService images)
    {
        _slides = slides;
        _images = images;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        ViewData["ActiveSlideCount"] = await _slides.CountActiveAsync();
        return View(await _slides.GetPagedAsync(page, 10));
    }

    [HttpGet]
    public IActionResult Create() => View(new HeroSlideFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HeroSlideFormViewModel vm)
    {
        vm.ImageUrl = null;
        ValidateLink(vm.LinkUrl);
        if (vm.ImageFile is null || vm.ImageFile.Length == 0)
            ModelState.AddModelError(nameof(vm.ImageFile), L10n.Text("请选择轮播图片。", "Choose an image for this slide."));
        if (!ModelState.IsValid) return View(vm);

        var imageUrl = await SaveImageAsync(vm.ImageFile!);
        if (imageUrl is null) return View(vm);

        var slide = new HeroSlide { ImageUrl = imageUrl };
        ApplyForm(slide, vm);
        try
        {
            await _slides.CreateAsync(slide);
        }
        catch
        {
            CleanupFailedUpload(imageUrl);
            throw;
        }

        TempData["Success"] = L10n.Text("轮播图已添加。", "Slide added.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var slide = await _slides.GetByIdAsync(id);
        if (slide is null) return NotFound();
        return View(new HeroSlideFormViewModel
        {
            Id = slide.Id,
            Title = slide.Title,
            TitleEn = slide.TitleEn,
            Heading = slide.Heading,
            HeadingEn = slide.HeadingEn,
            Description = slide.Description,
            DescriptionEn = slide.DescriptionEn,
            ImageUrl = slide.ImageUrl,
            ImageAlt = slide.ImageAlt,
            ImageAltEn = slide.ImageAltEn,
            LinkUrl = slide.LinkUrl,
            DisplayOrder = slide.DisplayOrder,
            IsActive = slide.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, HeroSlideFormViewModel vm)
    {
        if (id != vm.Id) return NotFound();
        var slide = await _slides.GetByIdAsync(id);
        if (slide is null) return NotFound();

        vm.ImageUrl = slide.ImageUrl;
        ValidateLink(vm.LinkUrl);
        if (!ModelState.IsValid) return View(vm);

        string? uploadedImageUrl = null;
        if (vm.ImageFile is not null)
        {
            uploadedImageUrl = await SaveImageAsync(vm.ImageFile);
            if (uploadedImageUrl is null) return View(vm);
        }

        ApplyForm(slide, vm);
        if (uploadedImageUrl is not null) slide.ImageUrl = uploadedImageUrl;
        try
        {
            await _slides.UpdateAsync(slide);
        }
        catch
        {
            if (uploadedImageUrl is not null) CleanupFailedUpload(uploadedImageUrl);
            throw;
        }

        TempData["Success"] = L10n.Text("轮播图已更新。", "Slide updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await _slides.DeleteAsync(id)) return NotFound();
        TempData["Success"] = L10n.Text("轮播图已删除。", "Slide deleted.");
        return RedirectToAction(nameof(Index));
    }

    private void ValidateLink(string? linkUrl)
    {
        if (string.IsNullOrWhiteSpace(linkUrl) || !linkUrl.StartsWith('/') ||
            linkUrl.Contains('\\') || linkUrl.Any(char.IsControl) || !Url.IsLocalUrl(linkUrl))
        {
            ModelState.AddModelError(nameof(HeroSlideFormViewModel.LinkUrl),
                L10n.Text("请输入以 / 开头的站内地址，例如 /Products。", "Enter a site URL beginning with /, such as /Products."));
        }
    }

    private async Task<string?> SaveImageAsync(IFormFile file)
    {
        try
        {
            return await _images.SaveAsync(file);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(HeroSlideFormViewModel.ImageFile), ex.Message);
            return null;
        }
    }

    private void CleanupFailedUpload(string imageUrl)
    {
        try { _images.Delete(imageUrl); }
        catch (IOException) {                                               }
        catch (UnauthorizedAccessException) {                                               }
    }

    private static void ApplyForm(HeroSlide slide, HeroSlideFormViewModel vm)
    {
        slide.Title = vm.Title.Trim();
        slide.TitleEn = TrimOptional(vm.TitleEn);
        slide.Heading = vm.Heading.Trim();
        slide.HeadingEn = TrimOptional(vm.HeadingEn);
        slide.Description = TrimOptional(vm.Description);
        slide.DescriptionEn = TrimOptional(vm.DescriptionEn);
        slide.ImageAlt = TrimOptional(vm.ImageAlt);
        slide.ImageAltEn = TrimOptional(vm.ImageAltEn);
        slide.LinkUrl = vm.LinkUrl;
        slide.DisplayOrder = vm.DisplayOrder;
        slide.IsActive = vm.IsActive;
    }

    private static string? TrimOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
