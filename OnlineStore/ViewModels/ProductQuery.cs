namespace OnlineStore.ViewModels;

/// <summary>商品列表查询条件（来自查询字符串，由模型绑定自动填充）。</summary>
public class ProductQuery
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 12;

    public int? CategoryId { get; set; }

    /// <summary>品牌筛选；当前数据模型中品牌由商品所属店铺表示。</summary>
    public int? ShopId { get; set; }

    public string? SearchTerm { get; set; }

    /// <summary>newest | price_asc | price_desc | name</summary>
    public string? SortBy { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    /// <summary>商品评价的平均最低评分（1~5）。</summary>
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
