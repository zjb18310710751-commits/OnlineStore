namespace OnlineStore.Services;

/// <summary>A shipping snapshot and charge to persist with the order in its existing transaction.</summary>
public sealed record OrderShippingQuote(
    string ShippingOrigin,
    string ShippingOriginEn,
    string ShippingFeeText,
    string ShippingFeeTextEn,
    string DispatchTimeText,
    string DispatchTimeTextEn,
    DateTime ExpectedDispatchAt,
    decimal ShippingAmount);
