using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
public class AdminBannersController : Controller
{
    private readonly ApplicationDbContext _db;
    public AdminBannersController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var settings = await GetOrCreateSettingsAsync();
        return View(new AdminBannersIndexVm
        {
            Banners = await _db.Banners.OrderBy(x => x.Position).ThenBy(x => x.SortOrder).ToListAsync(),
            Settings = new AdminSiteSettingsVm
            {
                SiteName = settings.SiteName,
                LogoUrl = settings.LogoUrl,
                DealSectionBackgroundUrl = settings.DealSectionBackgroundUrl,
                HotPromotionBackgroundUrl = settings.HotPromotionBackgroundUrl
            }
        });
    }
    [HttpGet] public IActionResult Create() => View(new Banner { IsActive = true, Position = "MainBanner" });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Banner model)
    {
        ValidateImageUrl(model.ImageUrl);
        if (!ModelState.IsValid) return View(model);
        _db.Banners.Add(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var banner = await _db.Banners.FindAsync(id);
        return banner == null ? NotFound() : View(banner);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Banner model)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner == null) return NotFound();
        ValidateImageUrl(model.ImageUrl);
        if (!ModelState.IsValid) return View(model);

        banner.Title = model.Title; banner.ImageUrl = model.ImageUrl.Trim(); banner.LinkUrl = model.LinkUrl;
        banner.Description = model.Description; banner.Position = model.Position; banner.SortOrder = model.SortOrder; banner.IsActive = model.IsActive;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSettings(AdminSiteSettingsVm vm)
    {
        var settings = await GetOrCreateSettingsAsync();
        var logoUrl = vm.LogoUrl?.Trim();
        var dealUrl = vm.DealSectionBackgroundUrl?.Trim();
        var hotUrl = vm.HotPromotionBackgroundUrl?.Trim();

        string? logoErr = null;
        string? dealErr = null;
        string? hotErr = null;

        if (!TryValidateOptionalHttpUrl(logoUrl, out logoErr) ||
            !TryValidateOptionalHttpUrl(dealUrl, out dealErr) ||
            !TryValidateOptionalHttpUrl(hotUrl, out hotErr))
        {
            TempData["SettingsMessage"] = logoErr ?? dealErr ?? hotErr ?? "URL ảnh không hợp lệ.";
            TempData["SettingsSuccess"] = false;
            return RedirectToAction(nameof(Index));
        }

        settings.LogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl;
        settings.DealSectionBackgroundUrl = string.IsNullOrWhiteSpace(dealUrl) ? null : dealUrl;
        settings.HotPromotionBackgroundUrl = string.IsNullOrWhiteSpace(hotUrl) ? null : hotUrl;
        settings.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["SettingsMessage"] = "Đã lưu cấu hình hình ảnh website.";
        TempData["SettingsSuccess"] = true;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> ToggleStatus(int id) { var banner = await _db.Banners.FindAsync(id); if (banner == null) return NotFound(); banner.IsActive = !banner.IsActive; await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Delete(int id) { var banner = await _db.Banners.FindAsync(id); if (banner != null) { _db.Banners.Remove(banner); await _db.SaveChangesAsync(); } return RedirectToAction(nameof(Index)); }

    private async Task<SiteSetting> GetOrCreateSettingsAsync()
    {
        await EnsureSiteSettingsTableAsync();
        var settings = await _db.SiteSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (settings is not null) return settings;
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

    private void ValidateImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) { ModelState.AddModelError(nameof(Banner.ImageUrl), "Vui lòng dán URL ảnh banner."); return; }
        if (!Uri.TryCreate(imageUrl.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            ModelState.AddModelError(nameof(Banner.ImageUrl), "URL ảnh banner không hợp lệ.");
    }
}
