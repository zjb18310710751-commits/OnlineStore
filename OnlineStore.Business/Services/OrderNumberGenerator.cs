namespace OnlineStore.Services;

public static class OrderNumberGenerator
{
    public static string Generate() =>
        $"OS{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid():N}".ToUpperInvariant()[..24];
}
