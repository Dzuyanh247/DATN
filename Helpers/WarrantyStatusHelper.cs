using Datn.PcStore.Models;

namespace Datn.PcStore.Helpers;

public static class WarrantyStatuses
{
    public const string Pending = "Chờ tiếp nhận";
    public const string Received = "Đã tiếp nhận";
    public const string Processing = "Đang xử lý";
    public const string Rejected = "Từ chối";
    public const string Completed = "Hoàn tất";

    public static readonly IReadOnlyList<string> All =
        [Pending, Received, Processing, Rejected, Completed];

    public static readonly IReadOnlyList<string> AllActive = [Pending, Received, Processing];

    public static bool IsActive(string? status) => status != null && AllActive.Contains(status);

    public static string BadgeClass(string? status) => status switch
    {
        Pending => "warranty-badge--pending",
        Received => "warranty-badge--received",
        Processing => "warranty-badge--processing",
        Rejected => "warranty-badge--rejected",
        Completed => "warranty-badge--completed",
        _ => "warranty-badge--pending"
    };
}

public static class WarrantyPolicy
{
    public static bool IsOrderEligible(Order order) =>
        order.Status == OrderStatus.Completed ||
        (order.Status is OrderStatus.Processing or OrderStatus.Delivering &&
         string.Equals(order.PaymentStatus, PaymentStatuses.Paid, StringComparison.OrdinalIgnoreCase));

    public static DateTime ExpiresAt(DateTime purchaseDate, int warrantyMonths) =>
        purchaseDate.AddMonths(Math.Max(1, warrantyMonths));

    public static bool IsInWarranty(DateTime purchaseDate, int warrantyMonths, DateTime utcNow) =>
        utcNow <= ExpiresAt(purchaseDate, warrantyMonths);
}
