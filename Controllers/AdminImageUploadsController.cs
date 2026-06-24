using Datn.PcStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin,Staff")]
[Route("AdminImageUploads")]
public class AdminImageUploadsController : Controller
{
    private readonly ICloudinaryImageUploadService _uploader;
    private readonly ILogger<AdminImageUploadsController> _logger;

    public AdminImageUploadsController(ICloudinaryImageUploadService uploader, ILogger<AdminImageUploadsController> logger)
    {
        _uploader = uploader;
        _logger = logger;
    }

    [HttpPost("Upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0) return BadRequest(new { success = false, message = "Vui lòng chọn file ảnh." });
        try
        {
            var url = await _uploader.UploadAsync(file, cancellationToken);
            return Ok(new { success = true, url, fileName = file.FileName, size = file.Length });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Admin image upload failed");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
