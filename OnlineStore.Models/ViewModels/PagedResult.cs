namespace OnlineStore.ViewModels;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
    public bool ShowPagination => TotalCount > PageSize;
}

public sealed class AdminPaginationViewModel
{
    public required string Action { get; init; }
    public required string Controller { get; init; }
    public int Page { get; init; }
    public int TotalPages { get; init; }
    public bool ShowWhenSinglePage { get; init; }
    public Dictionary<string, string?> RouteValues { get; init; } = new();
}
