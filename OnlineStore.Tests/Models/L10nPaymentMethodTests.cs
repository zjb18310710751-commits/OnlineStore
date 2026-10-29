using System.Globalization;
using OnlineStore.Models;
using Xunit;

namespace OnlineStore.Tests.Models;

public class L10nPaymentMethodTests
{
    [Fact]
    public void Options_DefineSupportedCheckoutMethodsOnce()
    {
        Assert.Equal(
            [
                PaymentMethods.CashOnDelivery,
                PaymentMethods.Card,
                PaymentMethods.Alipay,
                PaymentMethods.WeChatPay
            ],
            PaymentMethods.Options.Select(option => option.Code));

        Assert.All(PaymentMethods.Options, option => Assert.True(PaymentMethods.IsSupported(option.Code)));
        Assert.False(PaymentMethods.IsSupported(PaymentMethods.LegacyBankTransfer));
    }

    [Theory]
    [InlineData(PaymentMethods.CashOnDelivery, "货到付款", "Cash on delivery")]
    [InlineData(PaymentMethods.Card, "银行卡支付", "Card payment")]
    [InlineData(PaymentMethods.Alipay, "支付宝支付（Alipay+）", "Alipay+")]
    [InlineData(PaymentMethods.WeChatPay, "微信支付", "WeChat Pay")]
    public void PaymentMethod_LocalizesCheckoutOptions(
        string method,
        string chineseLabel,
        string englishLabel)
    {
        var originalCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("zh-CN");
            Assert.Equal(chineseLabel, L10n.PaymentMethod(method));

            CultureInfo.CurrentUICulture = new CultureInfo("en-GB");
            Assert.Equal(englishLabel, L10n.PaymentMethod(method));
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
