using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
public class AdminBannersController : Controller
{
    private readonly ApplicationDbContext _db;
    public AdminBannersController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(await _db.Banners.OrderBy(x => x.Position).ThenBy(x => x.SortOrder).ToListAsync());
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

    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> ToggleStatus(int id) { var banner = await _db.Banners.FindAsync(id); if (banner == null) return NotFound(); banner.IsActive = !banner.IsActive; await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Delete(int id) { var banner = await _db.Banners.FindAsync(id); if (banner != null) { _db.Banners.Remove(banner); await _db.SaveChangesAsync(); } return RedirectToAction(nameof(Index)); }

    private void ValidateImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) { ModelState.AddModelError(nameof(Banner.ImageUrl), "Vui lòng dán URL ảnh banner."); return; }
        if (!Uri.TryCreate(imageUrl.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            ModelState.AddModelError(nameof(Banner.ImageUrl), "URL ảnh banner không hợp lệ.");
    }
}
