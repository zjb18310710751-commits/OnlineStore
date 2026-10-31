using Microsoft.AspNetCore.Mvc;
using OnlineStore.Services;
using OnlineStore.ViewModels;

namespace OnlineStore.ViewComponents;

public class CategoryNavigationViewComponent : ViewComponent
{
    private readonly ICategoryService _categoryService;

    public CategoryNavigationViewComponent(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var categories = await _categoryService.GetNavigationAsync(HttpContext.RequestAborted);

        return View(new CategoryNavigationViewModel { Categories = categories });
    }
}
