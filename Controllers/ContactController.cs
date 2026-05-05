using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

public class ContactController : Controller
{
    private readonly ApplicationDbContext _db;
    public ContactController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost]
    public async Task<IActionResult> Index(Feedback model)
    {
        if (!ModelState.IsValid) return View(model);
        _db.Feedbacks.Add(model);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Cảm ơn bạn đã gửi phản hồi.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Manage() => View(await _db.Feedbacks.OrderByDescending(f => f.CreatedAt).ToListAsync());
}
