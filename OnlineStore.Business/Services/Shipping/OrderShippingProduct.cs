namespace OnlineStore.Services;

/// <summary>Immutable shipping facts; quantities, stock and tracked entities stay outside the policy.</summary>
public sealed record OrderShippingProduct(
    string? ShippingFrom,
    string? ShippingFromEn,
    string? ShippingFee,
    string? ShippingFeeEn,
    string? DeliveryTime,
    string? DeliveryTimeEn,
    decimal ShippingFeeAmount);
