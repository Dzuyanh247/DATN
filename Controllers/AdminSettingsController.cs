using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Settings")]
public class AdminSettingsController : Controller
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };
    private const long MaxFileSize = 5 * 1024 * 1024;

    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public AdminSettingsController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var settings = await GetOrCreateSettingsAsync();
        return View(new AdminSiteSettingsVm { SiteName = settings.SiteName, LogoUrl = settings.LogoUrl });
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(AdminSiteSettingsVm vm)
    {
        var settings = await GetOrCreateSettingsAsync();
        vm.SiteName = settings.SiteName;
        vm.LogoUrl = settings.LogoUrl;

        if (vm.LogoFile is null || vm.LogoFile.Length == 0)
        {
            vm.Message = "Vui lòng chọn file ảnh logo trước khi lưu.";
            return View(vm);
        }

        if (vm.LogoFile.Length > MaxFileSize)
        {
            vm.Message = "File logo vượt quá 5MB. Vui lòng chọn ảnh nhỏ hơn.";
            return View(vm);
        }

        var ext = Path.GetExtension(vm.LogoFile.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
        {
            vm.Message = "Định dạng không hợp lệ. Chỉ chấp nhận: jpg, jpeg, png, webp.";
            return View(vm);
        }

        if (!AllowedContentTypes.Contains(vm.LogoFile.ContentType))
        {
            vm.Message = "Content-Type không hợp lệ. Vui lòng chọn đúng ảnh JPG/PNG/WEBP.";
            return View(vm);
        }

        var logoDirectory = Path.Combine(_env.WebRootPath, "uploads", "logo");
        Directory.CreateDirectory(logoDirectory);

        var uniqueFileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var savePath = Path.Combine(logoDirectory, uniqueFileName);

        await using (var stream = System.IO.File.Create(savePath))
        {
            await vm.LogoFile.CopyToAsync(stream);
        }

        settings.LogoUrl = $"/uploads/logo/{uniqueFileName}";
        settings.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        vm.LogoUrl = settings.LogoUrl;
        vm.IsSuccess = true;
        vm.Message = "Đã lưu logo website thành công.";
        return View(vm);
    }

    private async Task<SiteSetting> GetOrCreateSettingsAsync()
    {
        var settings = await _db.SiteSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (settings is not null)
        {
            return settings;
        }

        settings = new SiteSetting { SiteName = "KKSHOP", LogoUrl = null };
        _db.SiteSettings.Add(settings);
        await _db.SaveChangesAsync();
        return settings;
    }
}
