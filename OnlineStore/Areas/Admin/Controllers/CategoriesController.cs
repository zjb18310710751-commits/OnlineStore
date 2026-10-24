using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.ViewModels;

namespace OnlineStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoriesController : Controller
{
    private readonly ICategoryService _categoryService;
    private readonly IStringLocalizer<SharedResource> _loc;

    public CategoriesController(ICategoryService categoryService, IStringLocalizer<SharedResource> localizer)
    {
        _categoryService = categoryService;
        _loc = localizer;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _categoryService.GetAllAsync();
        return View(categories);
    }

    [HttpGet]
    public IActionResult Create() => View(new CategoryFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var category = new Category
        {
            Name = vm.Name,
            NameEn = vm.NameEn,
            Slug = SlugHelper.ToSlug(vm.Name),
            Description = vm.Description,
            DescriptionEn = vm.DescriptionEn,
            DisplayOrder = vm.DisplayOrder
        };

        await _categoryService.CreateAsync(category);
        TempData["Success"] = _loc["CategoryCreated"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        return View(new CategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            NameEn = category.NameEn,
            Description = category.Description,
            DescriptionEn = category.DescriptionEn,
            DisplayOrder = category.DisplayOrder
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel vm)
    {
        if (id != vm.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var category = await _categoryService.GetByIdAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        category.Name = vm.Name;
        category.NameEn = vm.NameEn;
        category.Slug = SlugHelper.ToSlug(vm.Name);
        category.Description = vm.Description;
        category.DescriptionEn = vm.DescriptionEn;
        category.DisplayOrder = vm.DisplayOrder;

        await _categoryService.UpdateAsync(category);
        TempData["Success"] = _loc["CategoryUpdated"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _categoryService.DeleteAsync(id);
        TempData["Success"] = ok ? _loc["CategoryDeleted"].Value : null;
        TempData["Error"] = ok ? null : _loc["CategoryDeleteFailed"].Value;
        return RedirectToAction(nameof(Index));
    }
}
