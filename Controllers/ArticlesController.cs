using Datn.PcStore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

public class ArticlesController : Controller
{
    private readonly ApplicationDbContext _db;
    public ArticlesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? type)
    {
        var query = _db.Articles.Where(a => a.IsPublished).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(a => a.Type == type);
        }

        ViewBag.SelectedType = type;
        var categoryRaw = await _db.Articles
            .Where(a => a.IsPublished)
            .GroupBy(a => a.Type)
            .Select(g => new
            {
                Type = g.Key,
                Count = g.Count()
            })
            .OrderBy(x => x.Type)
            .ToListAsync();
        ViewBag.Categories = categoryRaw
            .Select(x => new ArticleCategorySummary(x.Type, x.Count))
            .ToList();
        ViewBag.LatestArticles = await _db.Articles
            .Where(a => a.IsPublished)
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .ToListAsync();

        var articles = await query
            .OrderByDescending(a => a.IsFeatured)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();

        return View(articles);
    }

    public async Task<IActionResult> Detail(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();

        var article = await _db.Articles.FirstOrDefaultAsync(a => a.Slug == slug && a.IsPublished);
        if (article == null && int.TryParse(slug, out var id))
        {
            article = await _db.Articles.FirstOrDefaultAsync(a => a.Id == id && a.IsPublished);
        }
        if (article == null) return NotFound();

        article.ViewCount++;
        await _db.SaveChangesAsync();

        ViewBag.RelatedArticles = await _db.Articles
            .Where(a => a.IsPublished && a.Id != article.Id && a.Type == article.Type)
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(3)
            .ToListAsync();

        ViewBag.PreviousArticle = await _db.Articles
            .Where(a => a.IsPublished && a.CreatedAt < article.CreatedAt)
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();
        ViewBag.NextArticle = await _db.Articles
            .Where(a => a.IsPublished && a.CreatedAt > article.CreatedAt)
            .AsNoTracking()
            .OrderBy(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        return View(article);
    }
}

public record ArticleCategorySummary(string Type, int Count);
