using Datn.PcStore.Models;

namespace Datn.PcStore.ViewModels;

public class AdminOrdersIndexVm
{
    public IReadOnlyList<AdminOrderRowVm> Orders { get; init; } = Array.Empty<AdminOrderRowVm>();
    public AdminOrderStatsVm Stats { get; init; } = new();
    public string? Search { get; init; }
    public OrderStatus? Status { get; init; }
    public string? PaymentStatus { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

public class AdminOrderStatsVm
{
    public int TotalOrders { get; init; }
    public int PendingPaymentOrders { get; init; }
    public int PaidOrders { get; init; }
    public int ExpiredPaymentOrders { get; init; }
}

public class AdminOrderRowVm
{
    public int Id { get; init; }
    public string OrderCode => $"DH{Id:D6}";
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public OrderStatus Status { get; init; }
    public string PaymentStatus { get; init; } = string.Empty;
    public DateTime? PaymentDeadline { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<AdminOrderProductVm> Products { get; init; } = Array.Empty<AdminOrderProductVm>();
}

public class AdminOrderProductVm
{
    public string Name { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal => Quantity * UnitPrice;
}

public class AdminOrderDetailVm
{
    public int Id { get; init; }
    public string OrderCode => $"DH{Id:D6}";
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public string ShippingAddress { get; init; } = string.Empty;
    public string? Note { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
    public DateTime? PaidAt { get; init; }
    public DateTime? PaymentDeadline { get; init; }
    public OrderStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public decimal SubtotalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal ShippingFee { get; init; }
    public decimal TotalAmount { get; init; }
    public IReadOnlyList<AdminOrderProductVm> Products { get; init; } = Array.Empty<AdminOrderProductVm>();
}
