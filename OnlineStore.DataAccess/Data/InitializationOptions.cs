namespace OnlineStore.Data;

public sealed class InitializationOptions
{
    public const string SectionName = "Initialization";

    public bool RunOnStartup { get; set; } = true;

    public bool ApplyMigrationsOnStartup { get; set; } = true;

    public int InitializationRetryCount { get; set; } = 2;

    public int InitializationRetryDelaySeconds { get; set; } = 2;

    public bool? FailFastOnInitializationFailure { get; set; }

    public bool SeedSampleCatalogOnEmpty { get; set; } = true;

    public string? AdminEmail { get; set; }

    public string? AdminPassword { get; set; }

    public bool SeedDemoData { get; set; }

    public string? DemoBuyerEmail { get; set; }

    public string? DemoBuyerPassword { get; set; }
}
