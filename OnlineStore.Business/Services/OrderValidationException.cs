namespace OnlineStore.Services;

public sealed class OrderValidationException : InvalidOperationException
{
    public OrderValidationException(string resourceKey, Exception? innerException = null)
        : base(resourceKey, innerException)
    {
        ResourceKey = resourceKey;
    }

    public string ResourceKey { get; }
}
