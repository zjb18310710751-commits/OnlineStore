namespace OnlineStore.Services;

public static class AddressFormatter
{
    public static string Combine(params string?[] parts) =>
        string.Join(", ", parts
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim()));
}
