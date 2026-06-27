using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

public class ArticlesController : Controller
{
    private readonly ApplicationDbContext _db;
    public ArticlesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? type)
    {
        var selectedType = string.IsNullOrWhiteSpace(type) ? null : ArticleTypes.Normalize(type);
        var isNewsHome = string.IsNullOrWhiteSpace(selectedType) || selectedType == ArticleTypes.TechNews;
        var query = _db.Articles.Where(a => a.IsPublished).AsNoTracking();
        if (!isNewsHome)
        {
            var aliases = ArticleTypes.GetStorageAliases(selectedType);
            query = query.Where(a => aliases.Contains(a.Type));
        }

        ViewBag.SelectedType = isNewsHome ? null : selectedType;
        ViewBag.IsNewsHome = isNewsHome;
        var publishedTypes = await _db.Articles
            .Where(a => a.IsPublished)
            .Select(a => a.Type)
            .ToListAsync();
        ViewBag.TotalPublishedArticles = publishedTypes.Count;
        ViewBag.Categories = publishedTypes
            .GroupBy(ArticleTypes.Normalize)
            .Select(g => new ArticleCategorySummary(g.Key, g.Count()))
            .OrderBy(x => ArticleTypes.Labels.TryGetValue(x.Type, out var label) ? label : x.Type)
            .ToList();
        ViewBag.LatestArticles = await _db.Articles
            .Where(a => a.IsPublished)
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .ToListAsync();

        var articles = await query
            .OrderByDescending(a => a.CreatedAt)
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

        var relatedAliases = ArticleTypes.GetStorageAliases(article.Type);
        ViewBag.RelatedArticles = await _db.Articles
            .Where(a => a.IsPublished && a.Id != article.Id && relatedAliases.Contains(a.Type))
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
