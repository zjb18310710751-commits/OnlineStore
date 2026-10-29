namespace OnlineStore.Services;

public sealed class OrderNumberGenerator : IOrderNumberGenerator
{
    private readonly TimeProvider _timeProvider;

    public OrderNumberGenerator(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public string Generate() =>
        $"OS{_timeProvider.GetUtcNow():yyyyMMddHHmmss}{Guid.NewGuid():N}".ToUpperInvariant()[..24];
}
