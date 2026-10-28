using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Models;
using OnlineStore.Services;
using OnlineStore.ViewModels;

namespace OnlineStore.Controllers;

public class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly IFavoriteService _favoriteService;
    private readonly IReviewService _reviewService;
    private readonly IOrderService _orderService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProductsController(
        IProductService productService,
        IFavoriteService favoriteService,
        IReviewService reviewService,
        IOrderService orderService,
        UserManager<ApplicationUser> userManager)
    {
        _productService = productService;
        _favoriteService = favoriteService;
        _reviewService = reviewService;
        _orderService = orderService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(ProductQuery query)
    {
        var vm = await _productService.GetPagedAsync(query);
        return View(vm);
    }

    public async Task<IActionResult> Details(int id, int? orderId = null)
    {
        var product = await _productService.GetByIdWithDetailsAsync(id);
        if (product is null || !product.IsActive)
        {
            return NotFound();
        }

        var summary = await _reviewService.GetProductSummaryAsync(id);
        var latestReviews = await _reviewService.GetProductReviewsPagedAsync(id, 1, 5, "all", "newest");
        ViewData["ReviewSummary"] = summary;
        ViewData["LatestReviews"] = latestReviews.Items;
        ViewData["ReviewCount"] = summary.TotalCount;

        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is not null)
            {
                ViewData["IsFavorite"] = await _favoriteService.IsFavoriteAsync(user.Id, id);
                var orders = await _orderService.GetByUserAsync(user.Id);
                var eligibleOrders = orders
                    .Where(order => order.Status == OrderStatus.Completed
                        && order.Items.Any(item => item.ProductId == id))
                    .ToList();
                var reviewOrder = orderId.HasValue
                    ? eligibleOrders.FirstOrDefault(order => order.Id == orderId.Value)
                    : eligibleOrders.FirstOrDefault();
                ViewData["CanReview"] = reviewOrder is not null;
                ViewData["ReviewOrderId"] = reviewOrder?.Id;
            }
        }

        return View(product);
    }

    public async Task<IActionResult> Reviews(
        int id,
        int page = 1,
        string filter = "all",
        string sort = "newest")
    {
        var product = await _productService.GetByIdWithDetailsAsync(id);
        if (product is null || !product.IsActive)
        {
            return NotFound();
        }

        filter = string.Equals(filter, "photos", StringComparison.OrdinalIgnoreCase) ? "photos" : "all";
        sort = sort.ToLowerInvariant() is "highest" or "lowest" ? sort.ToLowerInvariant() : "newest";
        var reviews = await _reviewService.GetProductReviewsPagedAsync(id, page, 10, filter, sort);
        var summary = await _reviewService.GetProductSummaryAsync(id);
        var ratingCounts = new int[6];
        foreach (var rating in summary.RatingCounts)
        {
            if (rating.Key is >= 1 and <= 5)
            {
                ratingCounts[rating.Key] = rating.Value;
            }
        }

        var viewModel = new ProductReviewsPageViewModel
        {
            Product = product,
            Reviews = reviews.Items,
            Page = reviews.Page,
            PageSize = reviews.PageSize,
            TotalCount = reviews.TotalCount,
            Filter = filter,
            Sort = sort,
            Average = summary.Average,
            PhotoCount = summary.PhotoCount,
            RatingCounts = ratingCounts
        };

        return string.Equals(Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
            ? PartialView("_ReviewArchive", viewModel)
            : View(viewModel);
    }
}
