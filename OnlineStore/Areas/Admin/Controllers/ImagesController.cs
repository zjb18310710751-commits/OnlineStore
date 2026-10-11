using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Services;

namespace OnlineStore.Areas.Admin.Controllers;

/// <summary>后台图片上传：供图文编辑器粘贴图片时调用，返回可访问的 URL。</summary>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ImagesController : Controller
{
    private readonly IImageStorageService _imageStorage;

    public ImagesController(IImageStorageService imageStorage)
    {
        _imageStorage = imageStorage;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "未收到文件" });
        }

        try
        {
            var url = await _imageStorage.SaveAsync(file);
            return Json(new { url });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
