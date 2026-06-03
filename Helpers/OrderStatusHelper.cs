using Datn.PcStore.Models;

namespace Datn.PcStore.Helpers;

public static class PaymentStatuses
{
    public const string Unpaid = "UNPAID";
    public const string Pending = "WAITING_PAYMENT";
    public const string PendingConfirmation = "WAITING_CONFIRMATION";
    public const string Paid = "PAID";
    public const string Failed = "FAILED";
    public const string Expired = "EXPIRED";
    public const string Refunded = "REFUNDED";
}

public static class PaymentMethods
{
    public const string Cod = "COD";
    public const string BankTransfer = "BANK_TRANSFER";

    public static bool IsCod(string? paymentMethod)
        => string.Equals(paymentMethod, Cod, StringComparison.OrdinalIgnoreCase);

    public static bool RequiresOnlinePayment(string? paymentMethod)
        => !IsCod(paymentMethod);

    public static string Label(string? paymentMethod) => paymentMethod switch
    {
        Cod => "COD - thanh toán khi nhận hàng",
        BankTransfer => "Chuyển khoản ngân hàng",
        null or "" => "Chưa chọn",
        _ => paymentMethod
    };
}

public static class OrderStatusHelper
{
    public static readonly TimeSpan PendingPaymentTimeToLive = TimeSpan.FromHours(2);

    public static string Label(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Chờ xác nhận",
        OrderStatus.PendingConfirmation => "Chờ xác nhận",
        OrderStatus.PendingPayment => "Chờ thanh toán",
        OrderStatus.Processing => "Đã xác nhận",
        OrderStatus.Delivering => "Đang giao",
        OrderStatus.Completed => "Hoàn thành",
        OrderStatus.Cancelled => "Đã hủy",
        OrderStatus.Expired => "Hết hạn thanh toán",
        _ => status.ToString()
    };

    public static string PaymentLabel(string? paymentStatus, OrderStatus? orderStatus = null) => paymentStatus switch
    {
        PaymentStatuses.Paid => "Đã thanh toán",
        PaymentStatuses.Pending => "Chờ thanh toán",
        PaymentStatuses.PendingConfirmation => "Chờ xác nhận thanh toán",
        PaymentStatuses.Unpaid => orderStatus == OrderStatus.PendingPayment ? "Chờ thanh toán" : "Chưa thanh toán",
        PaymentStatuses.Failed => "Thanh toán thất bại",
        PaymentStatuses.Expired => "Hết hạn thanh toán",
        PaymentStatuses.Refunded => "Đã hoàn tiền",
        _ when orderStatus == OrderStatus.Expired => "Hết hạn thanh toán",
        _ when orderStatus == OrderStatus.Cancelled => "Đã hủy",
        null or "" => "Chưa thanh toán",
        _ => paymentStatus!
    };

    public static bool IsPaid(Order order)
        => string.Equals(order.PaymentStatus, PaymentStatuses.Paid, StringComparison.OrdinalIgnoreCase);

    public static bool IsPendingPaymentStatus(string? paymentStatus)
        => string.Equals(paymentStatus, PaymentStatuses.Pending, StringComparison.OrdinalIgnoreCase)
           || string.Equals(paymentStatus, PaymentStatuses.Unpaid, StringComparison.OrdinalIgnoreCase);

    public static bool IsPendingPaymentOrder(Order order)
        => order.Status == OrderStatus.PendingPayment
           && PaymentMethods.RequiresOnlinePayment(order.PaymentMethod)
           && IsPendingPaymentStatus(order.PaymentStatus);

    public static bool IsExpiredPayment(Order order, DateTime utcNow)
        => order.Status == OrderStatus.Expired
           || string.Equals(order.PaymentStatus, PaymentStatuses.Expired, StringComparison.OrdinalIgnoreCase)
           || (IsPendingPaymentOrder(order)
               && order.PaymentExpireAt.HasValue
               && order.PaymentExpireAt.Value <= utcNow);

    public static bool CanPayNow(Order order, DateTime utcNow)
        => IsPendingPaymentOrder(order)
           && order.PaymentExpireAt.HasValue
           && order.PaymentExpireAt.Value > utcNow;

    public static int RemainingSeconds(Order order, DateTime utcNow)
    {
        if (!order.PaymentExpireAt.HasValue)
        {
            return 0;
        }

        return Math.Max(0, (int)Math.Floor((order.PaymentExpireAt.Value - utcNow).TotalSeconds));
    }
}
