using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
public class AdminOrdersController : Controller
{
    private readonly ApplicationDbContext _db;
    public AdminOrdersController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var orders = await _db.Orders.Include(o => o.User).OrderByDescending(o => o.CreatedAt).ToListAsync();
        return View(orders);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var order = await _db.Orders
            .Include(o => o.User)
            .Include(o => o.Details)
            .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();
        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();
        order.Status = status;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
