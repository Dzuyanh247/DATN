using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Settings")]
public class AdminSettingsController : Controller
{
    private readonly ApplicationDbContext _db;
    public AdminSettingsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        TempData["SettingsMessage"] = "Khu vực Cài đặt đã được chuyển sang Quản lý banner.";
        TempData["SettingsSuccess"] = true;
        return RedirectToAction("Index", "AdminBanners");
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public IActionResult Index(AdminSiteSettingsVm vm)
    {
        return RedirectToAction("Index", "AdminBanners");
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

    private static bool TryValidateOptionalHttpUrl(string? value, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed) ||
            (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
        {
            error = "URL ảnh không hợp lệ. Vui lòng dán URL tuyệt đối (http/https).";
            return false;
        }
        return true;
    }
}
