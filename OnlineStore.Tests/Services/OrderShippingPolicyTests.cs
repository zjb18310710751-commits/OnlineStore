using OnlineStore.Services;

namespace OnlineStore.Tests.Services;

public class OrderShippingPolicyTests
{
    private static readonly DateTime QuoteTime = new(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Quote_TrimsAndDeduplicatesEachLanguage_AndUsesSlowestDispatchEstimate()
    {
        var products = new[]
        {
            new OrderShippingProduct(" 剑桥 ", " Cambridge ", " 包邮 ", " Free ", "24小时", "24 hours", 0),
            new OrderShippingProduct("剑桥", "Cambridge", "包邮", "Free", "48小时", "48 hours", 0),
            new OrderShippingProduct("伦敦", "London", null, " ", "48小时", "48 hours", 0)
        };

        var result = new OrderShippingPolicy().Quote(products, false, QuoteTime);

        Assert.Equal("剑桥、伦敦", result.ShippingOrigin);
        Assert.Equal("Cambridge、London", result.ShippingOriginEn);
        Assert.Equal("包邮", result.ShippingFeeText);
        Assert.Equal("Free", result.ShippingFeeTextEn);
        Assert.Equal("24小时、48小时", result.DispatchTimeText);
        Assert.Equal("24 hours、48 hours", result.DispatchTimeTextEn);
        Assert.Equal(new DateTime(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc), result.ExpectedDispatchAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("发货时间待定")]
    [InlineData("999999999999小时")]
    public void Quote_UnknownDispatchTextUsesSeventyTwoHourFallback(string? dispatchText)
    {
        var products = new[] { Product(dispatchText, 0) };

        var result = new OrderShippingPolicy().Quote(products, false, QuoteTime);

        Assert.Equal(new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc), result.ExpectedDispatchAt);
    }

    [Theory]
    [InlineData(false, 7.5)]
    [InlineData(true, 0)]
    public void Quote_ChargesOnlyPositiveProductFees_UnlessShippingIsWaived(bool freeShipping, decimal expected)
    {
        var products = new[] { Product("24小时", 3), Product("24小时", 4.5m), Product("24小时", 0), Product("24小时", -2) };

        var result = new OrderShippingPolicy().Quote(products, freeShipping, QuoteTime);

        Assert.Equal(expected, result.ShippingAmount);
        Assert.Equal("配送费用", result.ShippingFeeText);
    }

    [Fact]
    public void Quote_TruncatesEverySnapshotField_WithoutSplittingSurrogatePairs()
    {
        var products = new[]
        {
            new OrderShippingProduct(new string('甲', 100), new string('A', 100),
                new string('费', 50), new string('F', 50), new string('时', 50), new string('H', 50), 0),
            new OrderShippingProduct(new string('乙', 97) + "🚚尾", new string('B', 100),
                new string('用', 47) + "🚚尾", new string('G', 50), new string('间', 50), new string('I', 50), 0)
        };

        var result = new OrderShippingPolicy().Quote(products, false, QuoteTime);

        Assert.Equal(new string('甲', 100) + "、" + new string('乙', 97) + "…", result.ShippingOrigin);
        Assert.Equal(new string('A', 100) + "、" + new string('B', 98) + "…", result.ShippingOriginEn);
        Assert.Equal(new string('费', 50) + "、" + new string('用', 47) + "…", result.ShippingFeeText);
        Assert.Equal(new string('F', 50) + "、" + new string('G', 48) + "…", result.ShippingFeeTextEn);
        Assert.Equal(new string('时', 50) + "、" + new string('间', 48) + "…", result.DispatchTimeText);
        Assert.Equal(new string('H', 50) + "、" + new string('I', 48) + "…", result.DispatchTimeTextEn);
    }

    private static OrderShippingProduct Product(string? dispatchText, decimal shippingFee) =>
        new("剑桥", "Cambridge", "配送费用", "Shipping fee", dispatchText, "Dispatch estimate", shippingFee);
}
