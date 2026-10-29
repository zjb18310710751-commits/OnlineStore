using OnlineStore.Models;

namespace OnlineStore.ViewModels;

public sealed class ProductReviewsPageViewModel
{
    public required Product Product { get; init; }
    public required IReadOnlyList<Review> Reviews { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
    public string Filter { get; init; } = "all";
    public string Sort { get; init; } = "newest";
    public double Average { get; init; }
    public int[] RatingCounts { get; init; } = new int[6];
    public int PhotoCount { get; init; }
}
