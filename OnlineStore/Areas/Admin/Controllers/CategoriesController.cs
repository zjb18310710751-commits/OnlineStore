using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.ViewModels;

namespace OnlineStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoriesController : Controller
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
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
            Slug = SlugHelper.ToSlug(vm.Name),
            Description = vm.Description,
            DisplayOrder = vm.DisplayOrder
        };

        await _categoryService.CreateAsync(category);
        TempData["Success"] = "分类已创建。";
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
            Description = category.Description,
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
        category.Slug = SlugHelper.ToSlug(vm.Name);
        category.Description = vm.Description;
        category.DisplayOrder = vm.DisplayOrder;

        await _categoryService.UpdateAsync(category);
        TempData["Success"] = "分类已更新。";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _categoryService.DeleteAsync(id);
        TempData["Success"] = ok ? "分类已删除。" : null;
        TempData["Error"] = ok ? null : "删除失败：该分类下仍有商品。";
        return RedirectToAction(nameof(Index));
    }
}
