using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using OnlineStore.Services;

namespace OnlineStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ReviewsController : Controller
{
    private readonly IReviewService _reviewService;
    private readonly IStringLocalizer<SharedResource> _loc;

    public ReviewsController(IReviewService reviewService, IStringLocalizer<SharedResource> localizer)
    {
        _reviewService = reviewService;
        _loc = localizer;
    }

    public async Task<IActionResult> Index(int page = 1) =>
        View(await _reviewService.GetPagedAsync(page, 10));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _reviewService.DeleteAsync(id);
        TempData["Success"] = _loc["ReviewDeleted"].Value;
        return RedirectToAction(nameof(Index));
    }
}
