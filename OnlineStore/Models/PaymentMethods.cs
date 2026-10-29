namespace OnlineStore.Models;

public sealed record PaymentMethodOption(
    string Code,
    string LabelResourceKey,
    string HintResourceKey,
    string IconClass,
    string InputId);

public static class PaymentMethods
{
    public const string CashOnDelivery = "cash_on_delivery";
    public const string Card = "card_demo";
    public const string Alipay = "alipay_demo";
    public const string WeChatPay = "wechat_pay_demo";
    public const string LegacyBankTransfer = "bank_transfer_demo";

    public static IReadOnlyList<PaymentMethodOption> Options { get; } =
    [
        new(CashOnDelivery, "CashOnDelivery", "CashOnDeliveryHint", "bi bi-cash-coin", "paymentCash"),
        new(Card, "CardPaymentDemo", "CardPaymentHint", "bi bi-credit-card", "paymentCard"),
        new(Alipay, "AlipayPaymentDemo", "AlipayPaymentHint", "bi bi-phone", "paymentAlipay"),
        new(WeChatPay, "WeChatPaymentDemo", "WeChatPaymentHint", "bi bi-chat-dots", "paymentWeChat")
    ];

    public static bool IsSupported(string? code) =>
        Options.Any(option => string.Equals(option.Code, code, StringComparison.Ordinal));
}
