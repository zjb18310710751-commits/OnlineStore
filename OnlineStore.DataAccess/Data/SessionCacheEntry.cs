namespace OnlineStore.Data;

public sealed class SessionCacheEntry
{
    public string Id { get; set; } = string.Empty;

    public byte[] Value { get; set; } = [];

    public DateTimeOffset ExpiresAtTime { get; set; }

    public long? SlidingExpirationInSeconds { get; set; }

    public DateTimeOffset? AbsoluteExpiration { get; set; }
}
