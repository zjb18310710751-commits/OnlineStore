using Microsoft.AspNetCore.Http;

namespace OnlineStore.Services;

/// <summary>
/// 商品图片文件存储抽象。当前实现保存到本地 wwwroot/uploads，
/// 后续可替换为对象存储（如 OSS/S3）而无需改动控制器（可修改性）。
/// </summary>
public interface IImageStorageService
{
    /// <summary>
    /// 保存上传的图片，返回可访问的相对 URL（如 /uploads/xxx.jpg）。
    /// 文件不合法时抛出 <see cref="InvalidOperationException"/>。
    /// </summary>
    Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>删除已存储的本地图片（仅清理 /uploads/ 下的文件，用于更换图片时清理旧文件）。</summary>
    void Delete(string? relativeUrl);
}
