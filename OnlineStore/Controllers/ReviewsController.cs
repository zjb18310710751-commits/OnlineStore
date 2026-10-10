using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Models;
using OnlineStore.Services;

namespace OnlineStore.Controllers;

/// <summary>前台评价：登录用户可为商品提交星级评价。</summary>
[Authorize]
public class ReviewsController : Controller
{
    private readonly IReviewService _reviewService;
    private readonly IProductService _productService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReviewsController(
        IReviewService reviewService,
        IProductService productService,
        UserManager<ApplicationUser> userManager)
    {
        _reviewService = reviewService;
        _productService = productService;
        _userManager = userManager;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int productId, int rating, string? comment)
    {
        var product = await _productService.GetByIdAsync(productId);
        if (product is null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);

        var review = new Review
        {
            ProductId = productId,
            UserId = user?.Id,
            UserName = ResolveUserName(user),
            Rating = Math.Clamp(rating, 1, 5),
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim()
        };

        await _reviewService.CreateAsync(review);
        TempData["Success"] = "评价已提交，感谢分享！";
        return RedirectToAction("Details", "Products", new { id = productId });
    }

    private static string ResolveUserName(ApplicationUser? user)
    {
        if (user is null)
        {
            return "匿名用户";
        }

        var fullName = $"{user.LastName}{user.FirstName}".Trim();
        return !string.IsNullOrWhiteSpace(fullName)
            ? fullName
            : user.Email?.Split('@')[0] ?? "用户";
    }
}
