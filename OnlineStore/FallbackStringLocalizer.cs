using Microsoft.Extensions.Localization;

namespace OnlineStore;

/// <summary>
/// 回退本地化器：当未注入 <see cref="IStringLocalizer{T}"/> 时（如单元测试），
/// 直接返回资源键本身，避免空引用。
/// </summary>
internal sealed class FallbackStringLocalizer<T> : IStringLocalizer<T>
{
    public static FallbackStringLocalizer<T> Instance { get; } = new();

    public LocalizedString this[string name] => new(name, name);

    public LocalizedString this[string name, params object[] arguments] =>
        new(name, string.Format(name, arguments));

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        Array.Empty<LocalizedString>();
}
