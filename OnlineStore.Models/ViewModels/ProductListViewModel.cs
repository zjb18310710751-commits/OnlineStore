using OnlineStore.Models;

namespace OnlineStore.ViewModels;

public class ProductListViewModel
{
    public IReadOnlyList<Product> Items { get; set; } = new List<Product>();

    public IReadOnlyList<Category> Categories { get; set; } = new List<Category>();

    public IReadOnlyList<Shop> Brands { get; set; } = new List<Shop>();

    public ProductQuery Query { get; set; } = new();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    public IReadOnlyList<Product> Recommendations { get; set; } = new List<Product>();

    public IReadOnlyList<Product> SaleItems { get; set; } = new List<Product>();
}
