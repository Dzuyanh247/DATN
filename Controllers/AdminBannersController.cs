using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
public class AdminBannersController : Controller
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private const long MaxFileSize = 5 * 1024 * 1024;
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public AdminBannersController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<IActionResult> Index() => View(await _db.Banners.OrderBy(x => x.Position).ThenBy(x => x.SortOrder).ToListAsync());

    [HttpGet]
    public IActionResult Create() => View(new Banner { IsActive = true, Position = "MainBanner" });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Banner model, IFormFile? ImageFile)
    {
        ValidateImageFile(ImageFile, true);
        if (!ModelState.IsValid) return View(model);

        model.ImageUrl = await SaveImageAsync(ImageFile!);
        _db.Banners.Add(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner == null) return NotFound();
        return View(banner);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Banner model, IFormFile? ImageFile)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner == null) return NotFound();

        ValidateImageFile(ImageFile, false);
        if (!ModelState.IsValid) return View(model);

        banner.Title = model.Title;
        banner.LinkUrl = model.LinkUrl;
        banner.Description = model.Description;
        banner.Position = model.Position;
        banner.SortOrder = model.SortOrder;
        banner.IsActive = model.IsActive;

        if (ImageFile is not null && ImageFile.Length > 0)
        {
            banner.ImageUrl = await SaveImageAsync(ImageFile);
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner == null) return NotFound();
        banner.IsActive = !banner.IsActive;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner != null)
        {
            _db.Banners.Remove(banner);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private void ValidateImageFile(IFormFile? imageFile, bool required)
    {
        if (imageFile is null || imageFile.Length == 0)
        {
            if (required)
            {
                ModelState.AddModelError("ImageFile", "Vui lòng chọn ảnh banner.");
            }
            return;
        }

        if (imageFile.Length > MaxFileSize)
        {
            ModelState.AddModelError("ImageFile", "Ảnh vượt quá dung lượng 5MB.");
        }

        var ext = Path.GetExtension(imageFile.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
        {
            ModelState.AddModelError("ImageFile", "Chỉ chấp nhận ảnh jpg, jpeg, png, webp, gif.");
        }
    }

    private async Task<string> SaveImageAsync(IFormFile imageFile)
    {
        var uploadRoot = Path.Combine(_env.WebRootPath, "uploads", "banners");
        if (!Directory.Exists(uploadRoot))
        {
            Directory.CreateDirectory(uploadRoot);
        }

        var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadRoot, fileName);
        await using var stream = System.IO.File.Create(fullPath);
        await imageFile.CopyToAsync(stream);
        return $"/uploads/banners/{fileName}";
    }
}
