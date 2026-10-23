using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using OnlineStore.Services;

namespace OnlineStore.Areas.Admin.Controllers;

/// <summary>后台图片上传：供图文编辑器粘贴图片时调用，返回可访问的 URL。</summary>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ImagesController : Controller
{
    private readonly IImageStorageService _imageStorage;
    private readonly IStringLocalizer<SharedResource> _loc;

    public ImagesController(IImageStorageService imageStorage, IStringLocalizer<SharedResource> localizer)
    {
        _imageStorage = imageStorage;
        _loc = localizer;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = _loc["NoFileReceived"].Value });
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
