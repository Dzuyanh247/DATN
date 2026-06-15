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
    public required Order Order { get; init; }
    public int ReviewedProductCount { get; init; }

    public int ProductCount => Order.Details.Count;
    public int RemainingReviewCount => Math.Max(0, ProductCount - ReviewedProductCount);
    public bool HasProductsToReview => Order.Status == OrderStatus.Completed && RemainingReviewCount > 0;
    public bool HasReviewedAllProducts => Order.Status == OrderStatus.Completed
                                         && ProductCount > 0
                                         && RemainingReviewCount == 0;
}
