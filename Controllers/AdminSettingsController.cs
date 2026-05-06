using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Settings")]
public class AdminSettingsController : Controller
{
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
        return View(new AdminSiteSettingsVm { SiteName = settings.SiteName, LogoUrl = settings.LogoUrl, DealSectionBackgroundUrl = settings.DealSectionBackgroundUrl, HotPromotionBackgroundUrl = settings.HotPromotionBackgroundUrl });
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(AdminSiteSettingsVm vm, IFormFile? dealBackgroundFile, IFormFile? hotPromotionBackgroundFile)
    {
        var settings = await GetOrCreateSettingsAsync();
        vm.SiteName = settings.SiteName;
        var logoUrl = vm.LogoUrl?.Trim();
        Uri? parsedUri = null;

        if (!string.IsNullOrWhiteSpace(logoUrl) &&
            !Uri.TryCreate(logoUrl, UriKind.Absolute, out parsedUri))
        {
            vm.LogoUrl = logoUrl;
            vm.Message = "Logo URL không hợp lệ. Vui lòng dán URL tuyệt đối (https://...).";
            return View(vm);
        }

        if (!string.IsNullOrWhiteSpace(logoUrl) && parsedUri is not null &&
            parsedUri.Scheme != Uri.UriSchemeHttp && parsedUri.Scheme != Uri.UriSchemeHttps)
        {
            vm.LogoUrl = logoUrl;
            vm.Message = "Logo URL chỉ hỗ trợ giao thức http/https.";
            return View(vm);
        }

        settings.LogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl;
        if (dealBackgroundFile is not null && dealBackgroundFile.Length > 0)
        {
            settings.DealSectionBackgroundUrl = await SaveSettingsImageAsync(dealBackgroundFile, "deal");
        }

        if (hotPromotionBackgroundFile is not null && hotPromotionBackgroundFile.Length > 0)
        {
            settings.HotPromotionBackgroundUrl = await SaveSettingsImageAsync(hotPromotionBackgroundFile, "hotpromotion");
        }

        settings.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        vm.LogoUrl = settings.LogoUrl;
        vm.DealSectionBackgroundUrl = settings.DealSectionBackgroundUrl;
        vm.HotPromotionBackgroundUrl = settings.HotPromotionBackgroundUrl;
        vm.IsSuccess = true;
        vm.Message = "Đã lưu cấu hình giao diện thành công.";
        return View(vm);
    }

    private async Task<SiteSetting> GetOrCreateSettingsAsync()
    {
        await EnsureSiteSettingsTableAsync();

        var settings = await _db.SiteSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (settings is not null)
        {
            return settings;
        }

        settings = new SiteSetting { SiteName = "KKSHOP", LogoUrl = null, DealSectionBackgroundUrl = null, HotPromotionBackgroundUrl = null };
        _db.SiteSettings.Add(settings);
        await _db.SaveChangesAsync();
        return settings;
    }

    private async Task EnsureSiteSettingsTableAsync()
    {
        const string sql = @"
IF OBJECT_ID(N'[dbo].[SiteSettings]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SiteSettings]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [SiteName] NVARCHAR(255) NOT NULL,
        [LogoUrl] NVARCHAR(1000) NULL,
        [DealSectionBackgroundUrl] NVARCHAR(1000) NULL,
        [HotPromotionBackgroundUrl] NVARCHAR(1000) NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NULL
    );
END";

        const string addColumnsSql = @"
IF COL_LENGTH('dbo.SiteSettings', 'DealSectionBackgroundUrl') IS NULL
    ALTER TABLE [dbo].[SiteSettings] ADD [DealSectionBackgroundUrl] NVARCHAR(1000) NULL;
IF COL_LENGTH('dbo.SiteSettings', 'HotPromotionBackgroundUrl') IS NULL
    ALTER TABLE [dbo].[SiteSettings] ADD [HotPromotionBackgroundUrl] NVARCHAR(1000) NULL;";

        try
        {
            await _db.Database.ExecuteSqlRawAsync(sql);
            await _db.Database.ExecuteSqlRawAsync(addColumnsSql);
        }
        catch (SqlException)
        {
            // Nếu không có quyền DDL thì bỏ qua để dùng schema hiện tại.
            // Các lỗi runtime còn lại sẽ được surfaced tại query phía dưới.
        }
    }

    private async Task<string> SaveSettingsImageAsync(IFormFile file, string prefix)
    {
        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{prefix}-bg-{DateTime.UtcNow:yyyyMMddHHmmssfff}{ext}";
        var folder = Path.Combine(_env.WebRootPath, "uploads", "settings");
        Directory.CreateDirectory(folder);
        var fullPath = Path.Combine(folder, fileName);
        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream);
        return $"/uploads/settings/{fileName}";
    }
}
