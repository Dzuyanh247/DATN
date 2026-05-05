using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
public class AdminCategoriesController : Controller
{
    private readonly ApplicationDbContext _db;
    public AdminCategoriesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(await _db.Categories.OrderBy(x => x.Name).ToListAsync());

    [HttpGet]
    public IActionResult Create() => View(new Category());

    [HttpPost]
    public async Task<IActionResult> Create(Category model)
    {
        if (!ModelState.IsValid) return View(model);
        _db.Categories.Add(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound();
        return View(category);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Category model)
    {
        if (!ModelState.IsValid) return View(model);
        _db.Categories.Update(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var hasProducts = await _db.Products.AnyAsync(p => p.CategoryId == id);
        if (hasProducts)
        {
            TempData["Err"] = "Không thể xóa danh mục đang có sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        var category = await _db.Categories.FindAsync(id);
        if (category != null)
        {
            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
