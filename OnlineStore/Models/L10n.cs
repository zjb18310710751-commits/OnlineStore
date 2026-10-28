using System.Globalization;

namespace OnlineStore.Models;

public static class L10n
{

    public static bool IsEnglish =>
        !CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase);

    public static string Name(Product? p) =>
        p is null ? string.Empty : (IsEnglish && !string.IsNullOrWhiteSpace(p.NameEn) ? p.NameEn : p.Name);

    public static string Description(Product? p) =>
        p is null ? string.Empty : (IsEnglish && !string.IsNullOrWhiteSpace(p.DescriptionEn) ? p.DescriptionEn : p.Description ?? string.Empty);

    public static string Name(Category? c) =>
        c is null ? string.Empty : (IsEnglish && !string.IsNullOrWhiteSpace(c.NameEn) ? c.NameEn : c.Name);

    public static string Description(Category? c) =>
        c is null ? string.Empty : (IsEnglish && !string.IsNullOrWhiteSpace(c.DescriptionEn) ? c.DescriptionEn : c.Description ?? string.Empty);

    public static string Name(Shop? s) =>
        s is null ? string.Empty : (IsEnglish && !string.IsNullOrWhiteSpace(s.NameEn) ? s.NameEn : s.Name);

    public static string Description(Shop? s) =>
        s is null ? string.Empty : (IsEnglish && !string.IsNullOrWhiteSpace(s.DescriptionEn) ? s.DescriptionEn : s.Description ?? string.Empty);

    public static string DetailHtml(Product? p) =>
        p is null ? string.Empty
        : (IsEnglish && !string.IsNullOrWhiteSpace(p.DetailHtmlEn) ? p.DetailHtmlEn : p.DetailHtml ?? string.Empty);

    public static string Text(string zh, string? en) =>
        IsEnglish && !string.IsNullOrWhiteSpace(en) ? en : zh;

    public static string Carrier(string? carrier)
    {
        if (!IsEnglish || string.IsNullOrWhiteSpace(carrier))
        {
            return carrier ?? string.Empty;
        }

        return carrier switch
        {
            "顺丰速运" => "SF Express",
            "中通快递" => "ZTO Express",
            "圆通速递" => "YTO Express",
            "邮政 EMS" => "EMS",
            "系统默认" => "System default",
            _ => carrier
        };
    }

    public static string PaymentMethod(string? method) =>
        method switch
        {
            "cash_on_delivery" => Text("货到付款", "Cash on delivery"),
            "card_demo" => Text("银行卡支付", "Card payment"),
            "alipay_demo" => Text("支付宝支付（Alipay+）", "Alipay+"),
            "wechat_pay_demo" => Text("微信支付", "WeChat Pay"),
            "bank_transfer_demo" => Text("银行转账", "Bank transfer"),
            _ => method ?? string.Empty
        };

    public static string AfterSalesStatusLabel(string status)
    {
        if (!IsEnglish)
        {
            return status;
        }

        return status switch
        {
            AfterSalesStatus.Pending => "Pending review",
            AfterSalesStatus.Approved => "Approved",
            AfterSalesStatus.Rejected => "Rejected",
            AfterSalesStatus.Returning => "Returning",
            AfterSalesStatus.Received => "Received",
            AfterSalesStatus.Completed => "Completed",
            _ => status
        };
    }

    public static string Status(string zh) =>
        IsEnglish
            ? zh switch
            {
                OrderStatus.PendingShipment => "Pending shipment",
                OrderStatus.Shipped => "Shipped",
                OrderStatus.Delivered => "Delivered",
                OrderStatus.Completed => "Completed",
                OrderStatus.Cancelled => "Cancelled",
                OrderStatus.Refunded => "Refunded",
                _ => zh
            }
            : zh;
}
