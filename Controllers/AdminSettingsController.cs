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
        settings.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        vm.LogoUrl = settings.LogoUrl;
        vm.IsSuccess = true;
        vm.Message = "Đã lưu Logo URL thành công.";
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

        settings = new SiteSetting { SiteName = "KKSHOP", LogoUrl = null };
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
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NULL
    );
END";

        try
        {
            await _db.Database.ExecuteSqlRawAsync(sql);
        }
        catch (SqlException)
        {
            // Nếu không có quyền DDL thì bỏ qua để dùng schema hiện tại.
            // Các lỗi runtime còn lại sẽ được surfaced tại query phía dưới.
        }
    }
}
