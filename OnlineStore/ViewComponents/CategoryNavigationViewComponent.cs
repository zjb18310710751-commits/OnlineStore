using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Data;
using OnlineStore.ViewModels;

namespace OnlineStore.ViewComponents;

public class CategoryNavigationViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;

    public CategoryNavigationViewComponent(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .Select(category => new CategoryNavigationCategory
            {
                Id = category.Id,
                Name = category.Name,
                NameEn = category.NameEn,
                Products = category.Products
                    .Where(product => product.IsActive)
                    .OrderByDescending(product => product.CreatedAt)
                    .ThenByDescending(product => product.Id)
                    .Take(12)
                    .Select(product => new CategoryNavigationProduct
                    {
                        Id = product.Id,
                        Name = product.Name,
                        NameEn = product.NameEn
                    })
                    .ToList()
            })
            .ToListAsync(HttpContext.RequestAborted);

        return View(new CategoryNavigationViewModel { Categories = categories });
    }
}
