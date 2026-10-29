using System.Text.RegularExpressions;

namespace OnlineStore.Services;

/// <summary>The existing checkout shipping rules, isolated from order persistence and inventory changes.</summary>
public sealed class OrderShippingPolicy : IOrderShippingPolicy
{
    public OrderShippingQuote Quote(IReadOnlyList<OrderShippingProduct> products, bool freeShipping, DateTime utcNow)
    {
        var dispatchHours = products.Select(product => ParseDispatchHours(product.DeliveryTime))
            .DefaultIfEmpty(72).Max();
        var shippingAmount = freeShipping
            ? 0
            : products.Select(product => product.ShippingFeeAmount).Where(amount => amount > 0).Sum();

        return new OrderShippingQuote(
            JoinDistinct(products.Select(product => product.ShippingFrom), 200),
            JoinDistinct(products.Select(product => product.ShippingFromEn), 200),
            JoinDistinct(products.Select(product => product.ShippingFee), 100),
            JoinDistinct(products.Select(product => product.ShippingFeeEn), 100),
            JoinDistinct(products.Select(product => product.DeliveryTime), 100),
            JoinDistinct(products.Select(product => product.DeliveryTimeEn), 100),
            utcNow.AddHours(dispatchHours),
            shippingAmount);
    }

    private static string JoinDistinct(IEnumerable<string?> values, int maxLength)
    {
        var text = string.Join("、", values.Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct());
        if (text.Length <= maxLength)
        {
            return text;
        }

        var prefixLength = maxLength - 1;
        if (char.IsHighSurrogate(text[prefixLength - 1]) && char.IsLowSurrogate(text[prefixLength]))
        {
            prefixLength--;
        }
        return text[..prefixLength] + "…";
    }

    private static int ParseDispatchHours(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 72;
        }

        var match = Regex.Match(text, @"\d+");
        return match.Success && int.TryParse(match.Value, out var hours) ? hours : 72;
    }
}
