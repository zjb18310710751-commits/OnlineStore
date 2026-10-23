namespace OnlineStore.Services;

/// <summary>将分段地址字段组合为订单和用户资料使用的单行地址。</summary>
public static class AddressFormatter
{
    public static string Combine(params string?[] parts) =>
        string.Join(", ", parts
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim()));
}
