using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace OnlineStore.Services;

public static class UploadStoragePath
{
    public const string RequestPath = "/uploads";
    public const string ConfigurationKey = "Storage:UploadsPath";

    public static string Resolve(IWebHostEnvironment environment, IConfiguration? configuration = null)
    {
        var configuredPath = configuration?[ConfigurationKey];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetFullPath(
                configuredPath,
                environment.ContentRootPath);
        }

        if (environment.IsProduction())
        {
            var homePath = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrWhiteSpace(homePath))
            {
                return Path.GetFullPath(Path.Combine(homePath, "data", "uploads"));
            }
        }

        var webRoot = environment.WebRootPath
            ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        return Path.GetFullPath(Path.Combine(webRoot, "uploads"));
    }

    public static string Prepare(
        IWebHostEnvironment environment,
        IConfiguration? configuration = null,
        ILogger? logger = null)
    {
        var uploadsRoot = Resolve(environment, configuration);
        Directory.CreateDirectory(uploadsRoot);

        var webRoot = environment.WebRootPath
            ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var legacyRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads"));
        if (string.Equals(uploadsRoot, legacyRoot, StringComparison.OrdinalIgnoreCase)
            || !Directory.Exists(legacyRoot))
        {
            return uploadsRoot;
        }

        foreach (var legacyFile in Directory.EnumerateFiles(legacyRoot))
        {
            var destination = Path.Combine(uploadsRoot, Path.GetFileName(legacyFile));
            if (File.Exists(destination))
            {
                continue;
            }

            try
            {
                File.Copy(legacyFile, destination);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger?.LogWarning(
                    ex,
                    "Unable to migrate legacy upload {LegacyFile} to persistent storage.",
                    legacyFile);
            }
        }

        return uploadsRoot;
    }
}
