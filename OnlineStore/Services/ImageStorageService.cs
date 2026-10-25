using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;

namespace OnlineStore.Services;

public class ImageStorageService : IImageStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private readonly IWebHostEnvironment _env;
    private readonly IStringLocalizer<SharedResource> _loc;

    public ImageStorageService(IWebHostEnvironment env, IStringLocalizer<SharedResource>? localizer = null)
    {
        _env = env;
        _loc = localizer ?? FallbackStringLocalizer<SharedResource>.Instance;
    }

    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException(_loc["UnsupportedImageFormat", ext].Value);
        }

        if (file.Length <= 0)
        {
            throw new InvalidOperationException(_loc["ImageEmpty"].Value);
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException(_loc["ImageTooLarge"].Value);
        }

        if (!string.IsNullOrWhiteSpace(file.ContentType) &&
            !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(_loc["ImageInvalid"].Value);
        }

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(uploadsDir, fileName);

        await using var stream = new FileStream(fullPath, FileMode.CreateNew);
        await file.CopyToAsync(stream, cancellationToken);

        return $"/uploads/{fileName}";
    }

    public void Delete(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl) ||
            !relativeUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var fileName = relativeUrl["/uploads/".Length..];
        if (string.IsNullOrWhiteSpace(fileName) || fileName is "." or ".." ||
            fileName.Any(character => character is '/' or '\\' or ':' || char.IsControl(character)) ||
            fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return;
        }

        var fullPath = Path.Combine(_env.WebRootPath, "uploads", fileName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
