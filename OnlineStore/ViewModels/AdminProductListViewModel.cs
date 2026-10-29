using OnlineStore.Models;

namespace OnlineStore.ViewModels;

public sealed class AdminProductListViewModel
{
    public IReadOnlyList<Product> Products { get; init; } = [];

    public IReadOnlyList<Category> Categories { get; init; } = [];

    public string? Search { get; init; }

    public int? CategoryId { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int Total { get; init; }

    public int TotalPages { get; init; }
}
