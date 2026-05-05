using Datn.PcStore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
public class AdminWarrantyController : Controller
{
    private readonly ApplicationDbContext _db;
    public AdminWarrantyController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(await _db.WarrantyRequests.Include(x => x.Product).Include(x => x.User).OrderByDescending(x => x.CreatedAt).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var request = await _db.WarrantyRequests.FindAsync(id);
        if (request == null) return NotFound();
        request.Status = status;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
