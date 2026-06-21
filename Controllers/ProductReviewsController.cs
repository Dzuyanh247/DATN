using System.Security.Claims;
using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Datn.PcStore.Services;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize]
public class ProductReviewsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IProductReviewService _reviewService;
    public ProductReviewsController(ApplicationDbContext db, IProductReviewService reviewService) { _db = db; _reviewService = reviewService; }

    [HttpGet]
    public async Task<IActionResult> Create(int orderId, int productId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var detail = await _reviewService.FindEligibleOrderDetailAsync(userId, productId, orderId);
        if (detail == null)
        {
            TempData["ErrorMessage"] = "Sản phẩm không thuộc đơn hàng hoàn thành của bạn hoặc đã được đánh giá.";
            return RedirectToAction("Detail", "Orders", new { id = orderId });
        }

        return View(new CreateProductReviewVm
        {
            OrderId = orderId,
            ProductId = productId,
            ProductName = detail.ProductName ?? "Sản phẩm không xác định",
            ProductImage = detail.ProductImage ?? "/images/no-image.png",
            OrderCode = $"DH{detail.OrderId:D6}",
            PurchaseDate = detail.Order?.CreatedAt ?? detail.CreatedAt
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProductReviewVm vm)
    {
        vm.Comment = vm.Comment?.Trim() ?? string.Empty;
        if (!await _db.Products.AnyAsync(x => x.Id == vm.ProductId)) return NotFound();
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = ModelState.Values.SelectMany(x => x.Errors).FirstOrDefault()?.ErrorMessage ?? "Đánh giá không hợp lệ.";
            return RedirectToReviewOrProduct(vm);
        }
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var detail = await _reviewService.FindEligibleOrderDetailAsync(userId, vm.ProductId, vm.OrderId);
        if (detail == null)
        {
            TempData["ErrorMessage"] = "Bạn chỉ có thể đánh giá sản phẩm thuộc đơn hàng đã hoàn thành và chưa được đánh giá.";
            return RedirectToReviewOrProduct(vm);
        }
        _db.ProductReviews.Add(new ProductReview
        {
            ProductId = vm.ProductId, UserId = userId, OrderId = detail.OrderId, OrderDetailId = detail.Id,
            Rating = vm.Rating, Comment = vm.Comment, Status = ReviewStatus.Approved
        });
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "Sản phẩm trong đơn hàng này đã được đánh giá.";
            return RedirectToReviewOrProduct(vm);
        }
        TempData["SuccessMessage"] = "Cảm ơn bạn đã đánh giá sản phẩm.";
        return RedirectToAction("Detail", "Orders", new { id = detail.OrderId });
    }

    private IActionResult RedirectToReviewOrProduct(CreateProductReviewVm vm) =>
        vm.OrderId.HasValue
            ? RedirectToAction(nameof(Create), new { orderId = vm.OrderId, productId = vm.ProductId })
            : RedirectToProduct(vm.ProductId);

    private IActionResult RedirectToProduct(int productId) => Redirect($"/Products/Detail/{productId}#product-reviews");
}
