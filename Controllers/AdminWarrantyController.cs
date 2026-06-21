using Datn.PcStore.Data;
using Datn.PcStore.Helpers;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
public class AdminWarrantyController : Controller
{
    private readonly ApplicationDbContext _db;
    public AdminWarrantyController(ApplicationDbContext db) => _db = db;

    [HttpGet("/AdminWarranty")]
    public async Task<IActionResult> Index(string? status, string? search)
    {
        var query = _db.WarrantyRequests.AsNoTracking()
            .Include(x => x.Order)
            .Include(x => x.OrderDetail)
            .Include(x => x.Product)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var digits = new string(term.Where(char.IsDigit).ToArray());
            int.TryParse(digits, out var orderId);
            query = query.Where(x => (x.CustomerName != null && x.CustomerName.Contains(term)) || (x.Phone != null && x.Phone.Contains(term)) ||
                                     (x.WarrantyCode != null && x.WarrantyCode.Contains(term)) || (x.RequestCode != null && x.RequestCode.Contains(term)) || (x.ProductName != null && x.ProductName.Contains(term)) ||
                                     (orderId > 0 && x.OrderId == orderId));
        }
        return View(new AdminWarrantyIndexVm
        {
            Status = status,
            Search = search,
            Requests = await query.OrderByDescending(x => x.CreatedAt).ToListAsync()
        });
    }

    [HttpGet("/AdminWarranty/Detail/{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var request = await _db.WarrantyRequests.AsNoTracking()
            .Include(x => x.Order)
            .Include(x => x.OrderDetail)
            .Include(x => x.Product)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);
        return request == null ? NotFound() : View(request);
    }

    [HttpPost("/AdminWarranty/Update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(AdminWarrantyUpdateVm vm)
    {
        var request = await _db.WarrantyRequests.FindAsync(vm.Id);
        if (request == null) return NotFound();
        if (!WarrantyStatuses.All.Contains(vm.Status)) ModelState.AddModelError(nameof(vm.Status), "Trạng thái không hợp lệ.");
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            return RedirectToAction(nameof(Detail), new { id = vm.Id });
        }
        request.Status = vm.Status;
        request.AdminNote = string.IsNullOrWhiteSpace(vm.AdminNote) ? null : vm.AdminNote.Trim();
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã cập nhật yêu cầu bảo hành.";
        return RedirectToAction(nameof(Detail), new { id = vm.Id });
    }
}
