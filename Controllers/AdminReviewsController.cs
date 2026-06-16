using System.Security.Claims;
using Datn.PcStore.Data;
using Datn.PcStore.Helpers;
using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
public class AdminReviewsController : Controller
{
    private readonly ApplicationDbContext _db;
    public AdminReviewsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? keyword, int? rating, ReviewStatus? status, int? productId, DateTime? fromDate, DateTime? toDate)
    {
        var query = _db.ProductReviews.Include(x => x.Product).Include(x => x.User).Include(x => x.Order).AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword)) { keyword = keyword.Trim(); query = query.Where(x => x.Product!.Name.Contains(keyword) || x.User!.FullName.Contains(keyword) || x.Comment.Contains(keyword)); }
        if (rating.HasValue) query = query.Where(x => x.Rating == rating);
        if (status.HasValue) query = query.Where(x => x.Status == status);
        if (productId.HasValue) query = query.Where(x => x.ProductId == productId);
        if (fromDate.HasValue) query = query.Where(x => x.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(x => x.CreatedAt < toDate.Value.AddDays(1));
        return View(new AdminReviewIndexVm { Reviews = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(), Products = await _db.Products.OrderBy(x => x.Name).ToListAsync(), Keyword = keyword, Rating = rating, Status = status, ProductId = productId, FromDate = fromDate, ToDate = toDate });
    }

    public async Task<IActionResult> Detail(int id)
    {
        var review = await _db.ProductReviews.Include(x => x.Product).Include(x => x.User).Include(x => x.Order).ThenInclude(x => x!.Details).FirstOrDefaultAsync(x => x.Id == id);
        return review == null ? NotFound() : View(review);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, ReviewStatus status, string? adminReply)
    {
        var review = await _db.ProductReviews.FindAsync(id);
        if (review == null) return NotFound();
        review.Status = status;
        adminReply = adminReply?.Trim();
        if (adminReply?.Length > 1000) { TempData["ErrorMessage"] = "Phản hồi không được vượt quá 1000 ký tự."; return RedirectToAction(nameof(Detail), new { id }); }
        review.AdminReply = string.IsNullOrWhiteSpace(adminReply) ? null : adminReply;
        review.AdminRepliedAt = review.AdminReply == null ? null : DateTime.UtcNow;
        SetHandler(review);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã cập nhật đánh giá.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleVisibility(int id)
    {
        var review = await _db.ProductReviews.FindAsync(id);
        if (review == null) return NotFound();
        review.Status = review.Status == ReviewStatus.Hidden ? ReviewStatus.Approved : ReviewStatus.Hidden;
        SetHandler(review);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = review.Status == ReviewStatus.Hidden ? "Đã ẩn đánh giá." : "Đã hiển thị đánh giá.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, ReviewStatus status)
    {
        var review = await _db.ProductReviews.FindAsync(id);
        if (review == null) return NotFound();
        review.Status = status;
        SetHandler(review);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Đã cập nhật trạng thái: {ReviewStatusHelper.Label(status)}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await _db.ProductReviews.FindAsync(id);
        if (review == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đánh giá cần xoá.";
            return RedirectToAction(nameof(Index));
        }

        _db.ProductReviews.Remove(review);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã xoá đánh giá thành công.";
        return RedirectToAction(nameof(Index));
    }

    private void SetHandler(ProductReview review)
    {
        review.HandledByStaffId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
        review.HandledByStaffName = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Quản trị viên";
        review.HandledAt = DateTime.UtcNow;
    }
}
