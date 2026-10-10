namespace OnlineStore.ViewModels;

/// <summary>商品列表查询条件（来自查询字符串，由模型绑定自动填充）。</summary>
public class ProductQuery
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 12;

    public int? CategoryId { get; set; }

    public string? SearchTerm { get; set; }

    /// <summary>newest | price_asc | price_desc | name</summary>
    public string? SortBy { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }
}
