namespace OnlineStore.Data;

public sealed class InitializationOptions
{
    public const string SectionName = "Initialization";

    public bool ApplyMigrationsOnStartup { get; set; } = true;

    public bool SeedSampleCatalogOnEmpty { get; set; } = true;

    public string? AdminEmail { get; set; }

    public string? AdminPassword { get; set; }

    public bool SeedDemoData { get; set; }

    public string? DemoBuyerEmail { get; set; }

    public string? DemoBuyerPassword { get; set; }
}
