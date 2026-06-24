using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin,Staff")]
public class AdminVouchersController : Controller
{
    private readonly ApplicationDbContext _db;
    public AdminVouchersController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(await _db.Vouchers.OrderByDescending(x => x.CreatedAt).ToListAsync());
    [HttpGet] public IActionResult Create() => View(new Voucher { StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(1), Quantity = 100, IsActive = true });

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
        voucher.MaxDiscountAmount = model.MaxDiscountAmount; voucher.MinimumOrderAmount = model.MinimumOrderAmount; voucher.MaxOrderAmount = model.MaxOrderAmount; voucher.Quantity = model.Quantity;
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
        if (Request.Form.ContainsKey("UnlimitedQuantity")) model.Quantity = int.MaxValue;
        if (model.DiscountType == VoucherDiscountType.FixedAmount) model.MaxDiscountAmount = null;
        if (model.MaxOrderAmount.GetValueOrDefault() <= 0) model.MaxOrderAmount = null;

        if (string.IsNullOrWhiteSpace(model.Code)) ModelState.AddModelError(nameof(model.Code), "Vui lòng nhập mã voucher.");
        if (model.Code.Any(char.IsWhiteSpace)) ModelState.AddModelError(nameof(model.Code), "Mã voucher không được chứa khoảng trắng.");
        if (model.DiscountValue <= 0) ModelState.AddModelError(nameof(model.DiscountValue), "Giá trị giảm phải lớn hơn 0.");
        if (model.DiscountType == VoucherDiscountType.Percent && (model.DiscountValue < 1 || model.DiscountValue > 100)) ModelState.AddModelError(nameof(model.DiscountValue), "Phần trăm giảm phải từ 1 đến 100.");
        if (model.DiscountType == VoucherDiscountType.FixedAmount && model.DiscountValue < 1000) ModelState.AddModelError(nameof(model.DiscountValue), "Giá trị giảm tiền cố định phải từ 1.000 VNĐ.");
        if (model.DiscountType == VoucherDiscountType.Percent && model.MaxDiscountAmount.HasValue && model.MaxDiscountAmount <= 0) ModelState.AddModelError(nameof(model.MaxDiscountAmount), "Giảm tối đa phải lớn hơn 0 hoặc để trống.");
        if (model.MinimumOrderAmount < 0) ModelState.AddModelError(nameof(model.MinimumOrderAmount), "Giá trị đơn hàng tối thiểu không được âm.");
        if (model.MaxOrderAmount.HasValue && model.MaxOrderAmount <= model.MinimumOrderAmount) ModelState.AddModelError(nameof(model.MaxOrderAmount), "Đơn hàng tối đa phải lớn hơn đơn hàng tối thiểu hoặc để trống.");
        if (model.Quantity < 1) ModelState.AddModelError(nameof(model.Quantity), "Số lượng voucher phải từ 1 hoặc chọn không giới hạn.");
        if (model.Quantity < model.UsedCount) ModelState.AddModelError(nameof(model.Quantity), "Số lượng không được nhỏ hơn số lượt đã dùng.");
        if (model.MaxUsagePerUser.HasValue && model.MaxUsagePerUser < 1) ModelState.AddModelError(nameof(model.MaxUsagePerUser), "Số lần dùng tối đa mỗi tài khoản phải từ 1.");
        if (model.EndDate <= model.StartDate) ModelState.AddModelError(nameof(model.EndDate), "Ngày kết thúc phải sau ngày bắt đầu.");
        var exists = _db.Vouchers.Any(x => x.Code == model.Code && (!editingId.HasValue || x.Id != editingId.Value));
        if (exists) ModelState.AddModelError(nameof(model.Code), "Mã voucher đã tồn tại.");
    }
}
