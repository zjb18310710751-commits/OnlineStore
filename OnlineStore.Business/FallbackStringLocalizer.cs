using Microsoft.Extensions.Localization;

namespace OnlineStore;

internal sealed class FallbackStringLocalizer<T> : IStringLocalizer<T>
{
    public static FallbackStringLocalizer<T> Instance { get; } = new();

    public LocalizedString this[string name] => new(name, name);

    public LocalizedString this[string name, params object[] arguments] =>
        new(name, string.Format(name, arguments));

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        Array.Empty<LocalizedString>();
}
