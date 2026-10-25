namespace OnlineStore.ViewModels;

public class ProductQuery
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 12;

    public int? CategoryId { get; set; }

    public int? ShopId { get; set; }

    public string? SearchTerm { get; set; }

    public string? SortBy { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public double? MinRating { get; set; }

    public bool InStockOnly { get; set; }

    public bool HasActiveFilters =>
        CategoryId.HasValue
        || ShopId.HasValue
        || MinPrice.HasValue
        || MaxPrice.HasValue
        || MinRating.HasValue
        || InStockOnly
        || !string.IsNullOrWhiteSpace(SearchTerm)
        || (!string.IsNullOrWhiteSpace(SortBy) && SortBy != "newest");
}
