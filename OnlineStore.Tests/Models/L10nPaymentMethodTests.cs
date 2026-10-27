using System.Globalization;
using OnlineStore.Models;
using Xunit;

namespace OnlineStore.Tests.Models;

public class L10nPaymentMethodTests
{
    [Theory]
    [InlineData("cash_on_delivery", "货到付款", "Cash on delivery")]
    [InlineData("card_demo", "银行卡支付", "Card payment")]
    [InlineData("alipay_demo", "支付宝支付（Alipay+）", "Alipay+")]
    [InlineData("wechat_pay_demo", "微信支付", "WeChat Pay")]
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
