using OnlineStore.Models;

namespace OnlineStore.ViewModels;

public sealed class MyOrdersViewModel
{
    public IReadOnlyList<Order> Orders { get; init; } = [];

    public IReadOnlyDictionary<string, Review> Reviews { get; init; } =
        new Dictionary<string, Review>();
}
