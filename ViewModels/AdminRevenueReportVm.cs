using Datn.PcStore.Models;

namespace Datn.PcStore.ViewModels;

public class AdminRevenueReportVm
{
    public string Period { get; init; } = "today";
    public string PeriodLabel { get; init; } = "Hôm nay";
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public decimal ProductRevenue { get; init; }
    public decimal ShippingFee { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal RefundAmount { get; init; }
    public int CustomerPaidCount { get; init; }
    public int OrderCount { get; init; }
    public decimal AverageOrderValue => OrderCount == 0 ? 0 : TotalRevenue / OrderCount;
    public decimal TotalRevenue { get; init; }
    public IReadOnlyList<AdminRevenueChartPointVm> ChartPoints { get; init; } = [];
    public IReadOnlyList<AdminTopProductRevenueVm> TopProducts { get; init; } = [];
    public IReadOnlyList<AdminPaymentMethodRevenueVm> PaymentMethods { get; init; } = [];
    public IReadOnlyList<AdminOrderStatusRevenueVm> OrderStatuses { get; init; } = [];
    public IReadOnlyList<AdminRevenueOrderRowVm> Orders { get; init; } = [];
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 12;
    public int TotalItems { get; init; }
    public int TotalPages => TotalItems == 0 ? 1 : (int)Math.Ceiling(TotalItems / (double)PageSize);
}

public class AdminRevenueChartPointVm
{
    public string Label { get; init; } = string.Empty;
    public decimal Revenue { get; init; }
    public int OrderCount { get; init; }
}

public class AdminTopProductRevenueVm
{
    public string Name { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = "/images/placeholders/product.svg";
    public int Quantity { get; init; }
    public decimal Revenue { get; init; }
}

public class AdminPaymentMethodRevenueVm
{
    public string Method { get; init; } = string.Empty;
    public int OrderCount { get; init; }
    public decimal Revenue { get; init; }
}

public class AdminOrderStatusRevenueVm
{
    public OrderStatus Status { get; init; }
    public int OrderCount { get; init; }
    public decimal Revenue { get; init; }
}

public class AdminRevenueOrderRowVm
{
    public int Id { get; init; }
    public string OrderCode => $"DH{Id:D6}";
    public string CustomerName { get; init; } = string.Empty;
    public decimal ProductRevenue { get; init; }
    public decimal ShippingFee { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
    public OrderStatus Status { get; init; }
    public DateTime RevenueAt { get; init; }
}
