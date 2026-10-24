using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;

namespace OnlineStore.Services;

/// <summary>本地磁盘图片存储：将上传文件保存到 wwwroot/uploads，返回相对 URL。</summary>
public class ImageStorageService : IImageStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

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

        // 防御性校验：Content-Type 需为图片（浏览器可伪造，作为扩展名校验之外的补充）
        if (!string.IsNullOrWhiteSpace(file.ContentType) &&
            !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(_loc["ImageInvalid"].Value);
        }

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);

        // 使用 GUID 命名，避免路径注入与原文件名冲突
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
            return; // 只清理本地上传的文件，不触碰外部 URL
        }

        var relativePath = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_env.WebRootPath, relativePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
