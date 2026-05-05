using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

public class ArticlesController : Controller
{
    private readonly ApplicationDbContext _db;
    public ArticlesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(await _db.Articles.OrderByDescending(a => a.CreatedAt).ToListAsync());

    public async Task<IActionResult> Detail(string slug)
    {
        var article = await _db.Articles.FirstOrDefaultAsync(a => a.Slug == slug);
        if (article == null) return NotFound();
        return View(article);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet]
    public IActionResult Create() => View(new Article());

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost]
    public async Task<IActionResult> Create(Article model)
    {
        if (!ModelState.IsValid) return View(model);
        _db.Articles.Add(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var article = await _db.Articles.FindAsync(id);
        if (article == null) return NotFound();
        return View(article);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost]
    public async Task<IActionResult> Edit(Article model)
    {
        if (!ModelState.IsValid) return View(model);
        _db.Articles.Update(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
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
}
