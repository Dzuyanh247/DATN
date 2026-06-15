using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Services;

public interface IProductReviewService
{
    Task<ProductReviewSectionVm> GetSectionAsync(int productId, int? userId, int? rating = null);
    Task<OrderDetail?> FindEligibleOrderDetailAsync(int userId, int productId, int? orderId = null);
}

public class ProductReviewService : IProductReviewService
{
    private readonly ApplicationDbContext _db;
    public ProductReviewService(ApplicationDbContext db) => _db = db;

    public async Task<OrderDetail?> FindEligibleOrderDetailAsync(int userId, int productId, int? orderId = null)
    {
        var reviewedOrderIds = _db.ProductReviews.Where(x => x.UserId == userId && x.ProductId == productId).Select(x => x.OrderId);
        return await _db.OrderDetails
            .Include(x => x.Order)
            .Where(x => x.ProductId == productId && x.Order!.UserId == userId && x.Order.Status == OrderStatus.Completed)
            .Where(x => !orderId.HasValue || x.OrderId == orderId.Value)
            .Where(x => !reviewedOrderIds.Contains(x.OrderId))
            .OrderByDescending(x => x.Order!.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<ProductReviewSectionVm> GetSectionAsync(int productId, int? userId, int? rating = null)
    {
        var visible = _db.ProductReviews.AsNoTracking()
            .Where(x => x.ProductId == productId && x.Status == ReviewStatus.Approved);
        var all = await visible.Include(x => x.User).OrderByDescending(x => x.CreatedAt).ToListAsync();
        var vm = new ProductReviewSectionVm
        {
            ProductId = productId,
            IsAuthenticated = userId.HasValue,
            TotalCount = all.Count,
            AverageRating = all.Count == 0 ? 0 : Math.Round(all.Average(x => x.Rating), 1),
            RatingCounts = Enumerable.Range(1, 5).ToDictionary(star => star, star => all.Count(x => x.Rating == star))
        };
        vm.Reviews = all.Where(x => !rating.HasValue || x.Rating == rating.Value).Select(x => new ProductReviewItemVm
        {
            Id = x.Id, CustomerName = MaskName(x.User?.FullName), Rating = x.Rating, Comment = x.Comment,
            CreatedAt = x.CreatedAt, AdminReply = x.AdminReply
        }).ToList();
        if (userId.HasValue)
        {
            vm.HasPurchased = await _db.OrderDetails.AnyAsync(x => x.ProductId == productId && x.Order!.UserId == userId && x.Order.Status == OrderStatus.Completed);
            vm.HasReviewed = await _db.ProductReviews.AnyAsync(x => x.ProductId == productId && x.UserId == userId);
            var eligible = await FindEligibleOrderDetailAsync(userId.Value, productId);
            vm.CanReview = eligible != null;
            vm.EligibleOrderId = eligible?.OrderId;
        }
        return vm;
    }

    private static string MaskName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Khách hàng KKSHOP";
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var last = parts[^1];
        return parts.Length == 1 ? $"{last[0]}***" : $"{string.Join(' ', parts[..^1])} {last[0]}***";
    }
}
