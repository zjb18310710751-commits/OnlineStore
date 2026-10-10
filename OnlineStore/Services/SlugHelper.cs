using System.Text;
using System.Text.RegularExpressions;

namespace OnlineStore.Services;

/// <summary>生成 URL 友好的 Slug（保留中文等 Unicode 字符，仅规范分隔符）。</summary>
public static class SlugHelper
{
    public static string ToSlug(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (var c in input.Trim().ToLowerInvariant())
        {
            sb.Append(char.IsLetterOrDigit(c) || c == '-' ? c : '-');
        }

        var slug = Regex.Replace(sb.ToString(), "-{2,}", "-").Trim('-');
        return slug.Length > 100 ? slug[..100] : slug;
    }
}
