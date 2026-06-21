using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin,Staff")]
public class AdminArticlesController : Controller
{
    private const long MaxUploadBytes = 3 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public AdminArticlesController(ApplicationDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    public async Task<IActionResult> Index() => View(await _db.Articles.OrderByDescending(a => a.CreatedAt).ToListAsync());

    [HttpGet]
    public IActionResult Create() => View(new Article { Type = ArticleTypes.TechNews, IsPublished = true });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Article model, IFormFile? coverImageFile)
    {
        await PrepareArticleAsync(model, coverImageFile, null);
        if (!ModelState.IsValid) return View(model);
        _db.Articles.Add(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var article = await _db.Articles.FindAsync(id);
        return article == null ? NotFound() : View(article);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Article model, IFormFile? coverImageFile)
    {
        var article = await _db.Articles.FindAsync(id);
        if (article == null) return NotFound();

        var oldImage = article.CoverImageUrl;
        article.Title = model.Title;
        article.Slug = model.Slug;
        article.Type = model.Type;
        article.Excerpt = model.Excerpt;
        article.Content = model.Content;
        article.CoverImageUrl = model.CoverImageUrl;
        article.IsPublished = model.IsPublished;
        article.IsFeatured = model.IsFeatured;

        await PrepareArticleAsync(article, coverImageFile, oldImage);
        if (!ModelState.IsValid) return View(article);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var article = await _db.Articles.FindAsync(id);
        if (article != null)
        {
            _db.Articles.Remove(article);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PrepareArticleAsync(Article article, IFormFile? upload, string? oldImage)
    {
        article.Title = article.Title?.Trim() ?? string.Empty;
        article.Slug = string.IsNullOrWhiteSpace(article.Slug) ? ToSlug(article.Title) : ToSlug(article.Slug);
        article.Type = string.IsNullOrWhiteSpace(article.Type) ? ArticleTypes.TechNews : article.Type.Trim();
        article.Excerpt = article.Excerpt?.Trim();
        article.Content = article.Content?.Trim() ?? string.Empty;
        article.CoverImageUrl = article.CoverImageUrl?.Trim();

        if (string.IsNullOrWhiteSpace(article.Title)) ModelState.AddModelError(nameof(article.Title), "Vui lòng nhập tiêu đề bài viết.");
        if (string.IsNullOrWhiteSpace(article.Content)) ModelState.AddModelError(nameof(article.Content), "Vui lòng nhập nội dung bài viết.");

        if (upload != null && upload.Length > 0)
        {
            var extension = Path.GetExtension(upload.FileName);
            if (!AllowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("CoverImageUrl", "Ảnh chỉ hỗ trợ jpg, jpeg, png hoặc webp.");
                return;
            }
            if (upload.Length > MaxUploadBytes)
            {
                ModelState.AddModelError("CoverImageUrl", "Dung lượng ảnh tối đa 3MB.");
                return;
            }

            var uploadDir = Path.Combine(_environment.WebRootPath, "uploads", "articles");
            Directory.CreateDirectory(uploadDir);
            var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var filePath = Path.Combine(uploadDir, fileName);
            await using var stream = System.IO.File.Create(filePath);
            await upload.CopyToAsync(stream);
            article.CoverImageUrl = $"/uploads/articles/{fileName}";
        }
        else if (string.IsNullOrWhiteSpace(article.CoverImageUrl))
        {
            article.CoverImageUrl = oldImage;
        }
    }

    private static string ToSlug(string value)
    {
        var normalized = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark) builder.Append(ch == 'đ' ? 'd' : ch);
        }
        var slug = Regex.Replace(builder.ToString().Normalize(NormalizationForm.FormC), @"[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N")[..10] : slug;
    }
}
