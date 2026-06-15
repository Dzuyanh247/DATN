using System.ComponentModel.DataAnnotations;
using Datn.PcStore.Models;

namespace Datn.PcStore.ViewModels;

public class CreateProductReviewVm
{
    public int ProductId { get; set; }
    public int? OrderId { get; set; }
    [Range(1, 5, ErrorMessage = "Vui lòng chọn từ 1 đến 5 sao.")] public int Rating { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá.")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Nội dung đánh giá phải từ 10 đến 1000 ký tự.")]
    public string Comment { get; set; } = string.Empty;
}

public class ProductReviewItemVm
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? AdminReply { get; set; }
}

public class ProductReviewSectionVm
{
    public int ProductId { get; set; }
    public double AverageRating { get; set; }
    public int TotalCount { get; set; }
    public Dictionary<int, int> RatingCounts { get; set; } = Enumerable.Range(1, 5).ToDictionary(x => x, _ => 0);
    public List<ProductReviewItemVm> Reviews { get; set; } = [];
    public bool IsAuthenticated { get; set; }
    public bool CanReview { get; set; }
    public bool HasPurchased { get; set; }
    public bool HasReviewed { get; set; }
    public int? EligibleOrderId { get; set; }
}

public class AdminReviewIndexVm
{
    public List<ProductReview> Reviews { get; set; } = [];
    public string? Keyword { get; set; }
    public int? Rating { get; set; }
    public ReviewStatus? Status { get; set; }
    public int? ProductId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<Product> Products { get; set; } = [];
}
