using OnlineStore.Models;

namespace OnlineStore.ViewModels;

public sealed class StorefrontHeaderViewModel
{
    public IReadOnlyList<Category> Categories { get; init; } = [];

    public ApplicationUser? CurrentUser { get; init; }

    public int FavoriteCount { get; init; }

    public int ActiveItemCount { get; init; }

    public int AvailableCouponCount { get; init; }
}
