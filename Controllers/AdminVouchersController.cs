using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
public class AdminVouchersController : Controller
{
    private readonly ApplicationDbContext _db;
    public AdminVouchersController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(await _db.Vouchers.OrderByDescending(x => x.CreatedAt).ToListAsync());
    [HttpGet] public IActionResult Create() => View(new Voucher { StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(1), Quantity = 100, IsActive = true });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Voucher model)
    {
        NormalizeAndValidate(model);
        if (!ModelState.IsValid) return View(model);
        _db.Vouchers.Add(model);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã thêm voucher.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var voucher = await _db.Vouchers.FindAsync(id);
        return voucher == null ? NotFound() : View(voucher);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Voucher model)
    {
        var voucher = await _db.Vouchers.FindAsync(id);
        if (voucher == null) return NotFound();
        NormalizeAndValidate(model, id);
        if (!ModelState.IsValid) return View(model);
        voucher.Code = model.Code; voucher.Name = model.Name; voucher.DiscountType = model.DiscountType; voucher.DiscountValue = model.DiscountValue;
        voucher.MaxDiscountAmount = model.MaxDiscountAmount; voucher.MinimumOrderAmount = model.MinimumOrderAmount; voucher.Quantity = model.Quantity;
        voucher.MaxUsagePerUser = model.MaxUsagePerUser; voucher.StartDate = model.StartDate; voucher.EndDate = model.EndDate; voucher.IsActive = model.IsActive;
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã cập nhật voucher.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var voucher = await _db.Vouchers.FindAsync(id);
        if (voucher == null) return NotFound();
        voucher.IsActive = !voucher.IsActive;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var voucher = await _db.Vouchers.Include(x => x.Usages).FirstOrDefaultAsync(x => x.Id == id);
        if (voucher == null) return NotFound();
        if (voucher.Usages.Any()) voucher.IsActive = false; else _db.Vouchers.Remove(voucher);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private void NormalizeAndValidate(Voucher model, int? editingId = null)
    {
        model.Code = (model.Code ?? string.Empty).Trim().ToUpperInvariant();
        model.Name = (model.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(model.Code)) ModelState.AddModelError(nameof(model.Code), "Vui lòng nhập mã voucher.");
        if (model.DiscountValue <= 0) ModelState.AddModelError(nameof(model.DiscountValue), "Giá trị giảm phải lớn hơn 0.");
        if (model.DiscountType == VoucherDiscountType.Percent && model.DiscountValue > 100) ModelState.AddModelError(nameof(model.DiscountValue), "Phần trăm giảm không được vượt quá 100%.");
        if (model.Quantity < model.UsedCount) ModelState.AddModelError(nameof(model.Quantity), "Số lượng không được nhỏ hơn số lượt đã dùng.");
        if (model.EndDate < model.StartDate) ModelState.AddModelError(nameof(model.EndDate), "Ngày kết thúc phải sau ngày bắt đầu.");
        var exists = _db.Vouchers.Any(x => x.Code == model.Code && (!editingId.HasValue || x.Id != editingId.Value));
        if (exists) ModelState.AddModelError(nameof(model.Code), "Mã voucher đã tồn tại.");
    }
}
