using OnlineStore.Models;

namespace OnlineStore.ViewModels;

/// <summary>商品目录页视图模型。</summary>
public class ProductListViewModel
{
    public IReadOnlyList<Product> Items { get; set; } = new List<Product>();

    public IReadOnlyList<Category> Categories { get; set; } = new List<Category>();

    public ProductQuery Query { get; set; } = new();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    /// <summary>搜索无结果时的「猜你喜欢」推荐。</summary>
    public IReadOnlyList<Product> Recommendations { get; set; } = new List<Product>();
}
