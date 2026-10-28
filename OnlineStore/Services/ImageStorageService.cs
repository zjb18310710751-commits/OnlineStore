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

        await using (var headerStream = file.OpenReadStream())
        {
            if (!await HasValidImageSignatureAsync(headerStream, ext, cancellationToken))
            {
                throw new InvalidOperationException(_loc["ImageInvalid"].Value);
            }
        }

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(uploadsDir, fileName);

        await using var stream = new FileStream(fullPath, FileMode.CreateNew);
        await file.CopyToAsync(stream, cancellationToken);

        return $"/uploads/{fileName}";
    }

    private static async Task<bool> HasValidImageSignatureAsync(
        Stream stream,
        string extension,
        CancellationToken cancellationToken)
    {
        var header = new byte[12];
        var read = await stream.ReadAsync(header.AsMemory(), cancellationToken);
        if (read < 6)
        {
            return false;
        }

        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => header.AsSpan(0, 8).SequenceEqual(
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".gif" => header.AsSpan(0, 6).SequenceEqual("GIF87a"u8)
                || header.AsSpan(0, 6).SequenceEqual("GIF89a"u8),
            ".webp" => read >= 12
                && header.AsSpan(0, 4).SequenceEqual("RIFF"u8)
                && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
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
