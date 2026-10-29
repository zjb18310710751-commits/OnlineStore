using System.Text;
using System.Text.RegularExpressions;

namespace OnlineStore.Services;

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
