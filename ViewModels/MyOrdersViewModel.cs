using Datn.PcStore.Models;

namespace Datn.PcStore.ViewModels;

public class MyOrdersViewModel
{
    public List<MyOrderRowViewModel> Orders { get; init; } = [];
    public string Status { get; init; } = "all";
    public string Keyword { get; init; } = string.Empty;
    public Dictionary<string, int> StatusCounts { get; init; } = [];
}

public class MyOrderRowViewModel
{
    public required MyOrderSummaryViewModel Order { get; init; }
    public int ReviewedProductCount { get; init; }

    public int ProductCount => Order.Details.Count;
    public int RemainingReviewCount => Math.Max(0, ProductCount - ReviewedProductCount);
    public bool HasProductsToReview => Order.Status == OrderStatus.Completed && RemainingReviewCount > 0;
    public bool HasReviewedAllProducts => Order.Status == OrderStatus.Completed
                                         && ProductCount > 0
                                         && RemainingReviewCount == 0;
}

public class MyOrderSummaryViewModel
{
    public int Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public OrderStatus Status { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTime? PaymentExpireAt { get; init; }
    public List<MyOrderDetailViewModel> Details { get; init; } = [];
}

public class MyOrderDetailViewModel
{
    public int ProductId { get; init; }
    public int Quantity { get; init; }
    public string ProductName { get; init; } = "Sản phẩm không xác định";
    public string ProductImage { get; init; } = "/images/placeholders/product.svg";
}
