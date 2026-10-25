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

    public static string Status(string zh) =>
        IsEnglish
            ? zh switch
            {
                OrderStatus.PendingShipment => "Pending shipment",
                OrderStatus.Shipped => "Shipped",
                OrderStatus.Completed => "Completed",
                OrderStatus.Cancelled => "Cancelled",
                OrderStatus.Refunded => "Refunded",
                _ => zh
            }
            : zh;
}
