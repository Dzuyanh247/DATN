using System.Security.Claims;
using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize]
public class WarrantyController : Controller
{
    private readonly ApplicationDbContext _db;
    public WarrantyController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        ViewBag.Products = await _db.Products.OrderBy(p => p.Name).ToListAsync();
        var requests = await _db.WarrantyRequests.Include(x => x.Product).Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).ToListAsync();
        return View(requests);
    }

    [HttpPost]
    public async Task<IActionResult> Create(int productId, string issueDescription)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        _db.WarrantyRequests.Add(new WarrantyRequest { UserId = userId, ProductId = productId, IssueDescription = issueDescription, Status = "Mới tạo" });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
