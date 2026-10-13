using System.Globalization;

namespace OnlineStore.Models;

/// <summary>
/// 数据库内容（商品/分类/店铺名称与描述）的语言取值辅助。
/// 英文界面下优先返回 *_En 字段，否则回退到中文字段。
/// </summary>
public static class L10n
{
    /// <summary>当前界面是否为英文（非中文）。</summary>
    public static bool IsEnglish =>
        !CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase);

    public static string Name(Product? p) =>
        p is null ? string.Empty : (IsEnglish && !string.IsNullOrWhiteSpace(p.NameEn) ? p.NameEn : p.Name);

    public static string Description(Product? p) =>
        p is null ? string.Empty : (IsEnglish && !string.IsNullOrWhiteSpace(p.DescriptionEn) ? p.DescriptionEn : p.Description ?? string.Empty);

    public static string Name(Category? c) =>
        c is null ? string.Empty : (IsEnglish && !string.IsNullOrWhiteSpace(c.NameEn) ? c.NameEn : c.Name);

    public static string Name(Shop? s) =>
        s is null ? string.Empty : (IsEnglish && !string.IsNullOrWhiteSpace(s.NameEn) ? s.NameEn : s.Name);

    public static string Description(Shop? s) =>
        s is null ? string.Empty : (IsEnglish && !string.IsNullOrWhiteSpace(s.DescriptionEn) ? s.DescriptionEn : s.Description ?? string.Empty);

    /// <summary>图文详情：英文界面下优先返回 DetailHtmlEn，否则返回 DetailHtml。</summary>
    public static string DetailHtml(Product? p) =>
        p is null ? string.Empty
        : (IsEnglish && !string.IsNullOrWhiteSpace(p.DetailHtmlEn) ? p.DetailHtmlEn : p.DetailHtml ?? string.Empty);

    /// <summary>通用取值：英文界面下优先返回 en，否则返回 zh。</summary>
    public static string Text(string zh, string? en) =>
        IsEnglish && !string.IsNullOrWhiteSpace(en) ? en : zh;
}
