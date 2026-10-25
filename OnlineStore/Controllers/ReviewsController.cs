using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Controllers;

[Authorize]
public class ReviewsController : Controller
{
    private readonly IReviewService _reviewService;
    private readonly IProductService _productService;
    private readonly IImageStorageService _imageStorage;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<SharedResource> _loc;

    public ReviewsController(
        IReviewService reviewService,
        IProductService productService,
        IImageStorageService imageStorage,
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<SharedResource> localizer)
    {
        _reviewService = reviewService;
        _productService = productService;
        _imageStorage = imageStorage;
        _userManager = userManager;
        _loc = localizer;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int productId, int rating, string? comment, IFormFile? image)
    {
        var product = await _productService.GetByIdAsync(productId);
        if (product is null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (comment?.Length > 1000 || rating is < 1 or > 5 || !ModelState.IsValid)
        {
            TempData["Error"] = L10n.Text("请选择有效评分，评价内容不能超过 1000 个字符。",
                "Choose a valid rating and keep your review within 1,000 characters.");
            return RedirectToAction("Details", "Products", new { id = productId });
        }

        string? imageUrl = null;
        if (image is { Length: > 0 })
        {
            try
            {
                imageUrl = await _imageStorage.SaveAsync(image);
            }
            catch (InvalidOperationException)
            {
                imageUrl = null;
            }
        }

        var review = new Review
        {
            ProductId = productId,
            UserId = user?.Id,
            UserName = ResolveUserName(user),
            Rating = Math.Clamp(rating, 1, 5),
            Comment = comment,
            ImageUrl = imageUrl
        };

        await _reviewService.CreateAsync(review);
        TempData["Success"] = _loc["ReviewSubmitted"].Value;
        return RedirectToAction("Details", "Products", new { id = productId });
    }

    private static string ResolveUserName(ApplicationUser? user)
    {
        if (user is null)
        {
            return "匿名用户";
        }

        var fullName = $"{user.LastName}{user.FirstName}".Trim();
        var name = !string.IsNullOrWhiteSpace(fullName)
            ? fullName
            : user.Email?.Split('@')[0] ?? "用户";
        return name[..Math.Min(name.Length, 50)];
    }
}
