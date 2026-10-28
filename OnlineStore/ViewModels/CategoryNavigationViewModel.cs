namespace OnlineStore.ViewModels;

public class CategoryNavigationViewModel
{
    public IReadOnlyList<CategoryNavigationCategory> Categories { get; init; } = new List<CategoryNavigationCategory>();
}

public class CategoryNavigationCategory
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? NameEn { get; init; }
    public List<CategoryNavigationProduct> Products { get; init; } = new();
}

public class CategoryNavigationProduct
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? NameEn { get; init; }
}
