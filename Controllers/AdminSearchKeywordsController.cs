using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Datn.PcStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
public class AdminSearchKeywordsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ISearchKeywordService _keywordService;

    public AdminSearchKeywordsController(ApplicationDbContext db, ISearchKeywordService keywordService)
    {
        _db = db;
        _keywordService = keywordService;
    }

    public async Task<IActionResult> Index(string? keyword)
    {
        keyword = keyword?.Trim();
        var query = _db.SearchKeywords.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Keyword.Contains(keyword));
        ViewBag.Keyword = keyword;
        return View(await query.OrderByDescending(x => x.IsPinned).ThenByDescending(x => x.SearchCount).ThenByDescending(x => x.LastSearchedAt).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create(string keyword, bool isVisible = true, bool isPinned = false)
    {
        var normalized = _keywordService.NormalizeKeyword(keyword);
        if (string.IsNullOrWhiteSpace(normalized)) return RedirectToAction(nameof(Index));

        var existing = await _db.SearchKeywords.FirstOrDefaultAsync(x => x.Keyword == normalized);
        if (existing is null)
        {
            _db.SearchKeywords.Add(new SearchKeyword { Keyword = normalized, SearchCount = 0, LastSearchedAt = DateTime.UtcNow, IsVisible = isVisible, IsPinned = isPinned });
        }
        else
        {
            existing.IsVisible = isVisible;
            existing.IsPinned = isPinned;
        }
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, string keyword, int searchCount, bool isVisible = false, bool isPinned = false)
    {
        var item = await _db.SearchKeywords.FindAsync(id);
        if (item is null) return NotFound();
        var normalized = _keywordService.NormalizeKeyword(keyword);
        if (string.IsNullOrWhiteSpace(normalized)) return RedirectToAction(nameof(Index));
        var duplicate = await _db.SearchKeywords.AnyAsync(x => x.Id != id && x.Keyword == normalized);
        if (duplicate)
        {
            TempData["Err"] = "Từ khóa đã tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        item.Keyword = normalized;
        item.SearchCount = Math.Max(0, searchCount);
        item.IsVisible = isVisible;
        item.IsPinned = isPinned;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleVisible(int id)
    {
        var item = await _db.SearchKeywords.FindAsync(id);
        if (item is not null) { item.IsVisible = !item.IsVisible; await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> TogglePinned(int id)
    {
        var item = await _db.SearchKeywords.FindAsync(id);
        if (item is not null) { item.IsPinned = !item.IsPinned; await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ResetCount(int id)
    {
        var item = await _db.SearchKeywords.FindAsync(id);
        if (item is not null) { item.SearchCount = 0; await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.SearchKeywords.FindAsync(id);
        if (item is not null) { _db.SearchKeywords.Remove(item); await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }
}
