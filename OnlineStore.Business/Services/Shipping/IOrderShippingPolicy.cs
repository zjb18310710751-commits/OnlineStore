namespace OnlineStore.Services;

/// <summary>Calculates checkout shipping from distinct, revalidated products without persistence or clock access.</summary>
public interface IOrderShippingPolicy
{
    OrderShippingQuote Quote(IReadOnlyList<OrderShippingProduct> products, bool freeShipping, DateTime utcNow);
}
