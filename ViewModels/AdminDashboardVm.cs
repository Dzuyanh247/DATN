using Datn.PcStore.Models;

namespace Datn.PcStore.ViewModels;

public class AdminDashboardVm
{
    public int ProductCount { get; init; }
    public int OrderCount { get; init; }
    public int UserCount { get; init; }
    public int WarrantyRequestCount { get; init; }
    public int PendingOrderCount { get; init; }
    public int PendingPaymentCount { get; init; }
    public int BankTransferConfirmationCount { get; init; }
    public int NewWarrantyCount { get; init; }
    public int UnreadSupportCount { get; init; }
    public int LowStockCount { get; init; }
    public decimal TodayRevenue { get; init; }
    public decimal MonthRevenue { get; init; }
    public string AdminDisplayName { get; init; } = "Quản trị viên";
    public IReadOnlyList<AdminDashboardOrderVm> RecentOrders { get; init; } = [];
    public IReadOnlyList<AdminDashboardProductVm> AttentionProducts { get; init; } = [];
    public IReadOnlyList<AdminDashboardChartPointVm> RevenueLastSevenDays { get; init; } = [];
}

public class AdminDashboardOrderVm
{
    public int Id { get; init; }
    public string OrderCode => $"DH{Id:D6}";
    public string CustomerName { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public OrderStatus Status { get; init; }
    public string PaymentStatus { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string? ProductImage { get; init; }
}

public class AdminDashboardProductVm
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public string CategoryName { get; init; } = "Chưa phân loại";
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
    public bool IsActive { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public class AdminDashboardChartPointVm
{
    public DateTime Date { get; init; }
    public decimal Revenue { get; init; }
    public int OrderCount { get; init; }
}
