using Datn.PcStore.Data;
using Datn.PcStore.Helpers;
using Datn.PcStore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Datn.PcStore.Services;

public interface IOrderExpirationService
{
    Task<int> ExpirePendingPaymentOrdersAsync(CancellationToken cancellationToken = default);
    Task<bool> ExpireOrderIfNeededAsync(Order order, CancellationToken cancellationToken = default);
    void PreparePendingPayment(Order order, DateTime? utcNow = null, bool resetExpiredDeadline = false);
    void MarkPaymentConfirmedByCustomer(Order order, DateTime? utcNow = null);
    void MarkPaidByAdmin(Order order, DateTime? utcNow = null);
    Task MarkCancelledAsync(Order order, CancellationToken cancellationToken = default);
    Task MarkExpiredAsync(Order order, CancellationToken cancellationToken = default);
    Task MarkExpiredByAdminAsync(Order order, CancellationToken cancellationToken = default);
}

public class OrderExpirationService : IOrderExpirationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<OrderExpirationService> _logger;

    public OrderExpirationService(ApplicationDbContext db, ILogger<OrderExpirationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> ExpirePendingPaymentOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _db.Orders
            .Where(x => x.Status == OrderStatus.PendingPayment
                        && x.PaymentMethod != PaymentMethods.Cod
                        && x.PaymentStatus != PaymentStatuses.Paid)
            .Include(x => x.Details)
            .ToListAsync(cancellationToken);

        var expiredCount = 0;
        foreach (var order in orders)
        {
            if (await ExpireOrderIfNeededAsync(order, cancellationToken))
            {
                expiredCount++;
            }
        }

        return expiredCount;
    }

    public async Task<bool> ExpireOrderIfNeededAsync(Order order, CancellationToken cancellationToken = default)
    {
        var utcNow = DateTimeHelper.UtcNow();
        if (RestorePendingPaymentIfDeadlineStillValid(order, utcNow))
        {
            await _db.SaveChangesAsync(cancellationToken);
            return false;
        }

        if (!OrderStatusHelper.IsPendingPaymentOrder(order) || OrderStatusHelper.IsPaid(order))
        {
            return false;
        }

        EnsurePendingPaymentDeadline(order, utcNow);
        if (!order.PaymentExpireAt.HasValue || order.PaymentExpireAt.Value > utcNow)
        {
            if (_db.Entry(order).State == EntityState.Modified)
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            return false;
        }

        LogExpiration(order, utcNow, order.Status, PaymentStatuses.Expired);
        order.Status = OrderStatus.Expired;
        order.PaymentStatus = PaymentStatuses.Expired;
        await ReleaseReservedStockAsync(order, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public void PreparePendingPayment(Order order, DateTime? utcNow = null, bool resetExpiredDeadline = false)
    {
        var now = utcNow ?? DateTimeHelper.UtcNow();
        order.Status = OrderStatus.PendingPayment;
        order.PaymentStatus = PaymentStatuses.Pending;
        order.TransferContent = $"DH{order.Id:D6}";
        order.PaidAt = null;

        if (!order.PaymentExpireAt.HasValue || order.PaymentExpireAt.Value <= now || resetExpiredDeadline)
        {
            order.PaymentExpireAt = now.Add(OrderStatusHelper.PendingPaymentTimeToLive);
        }
    }

    public void MarkPaymentConfirmedByCustomer(Order order, DateTime? utcNow = null)
    {
        order.Status = OrderStatus.PendingConfirmation;
        order.PaymentStatus = PaymentStatuses.PendingConfirmation;
        order.PaymentExpireAt = null;
        order.PaidAt ??= utcNow ?? DateTimeHelper.UtcNow();
    }

    public void MarkPaidByAdmin(Order order, DateTime? utcNow = null)
    {
        order.PaymentStatus = PaymentStatuses.Paid;
        order.Status = OrderStatus.Processing;
        order.PaymentExpireAt = null;
        order.PaidAt ??= utcNow ?? DateTimeHelper.UtcNow();
    }

    public async Task MarkCancelledAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (order.Status != OrderStatus.Cancelled && order.Status != OrderStatus.Expired)
        {
            await ReleaseReservedStockAsync(order, cancellationToken);
        }

        var wasPaid = OrderStatusHelper.IsPaid(order);
        order.Status = OrderStatus.Cancelled;
        order.PaymentExpireAt = null;
        if (!wasPaid)
        {
            order.PaymentStatus = PaymentStatuses.Failed;
        }
    }

    public async Task MarkExpiredAsync(Order order, CancellationToken cancellationToken = default)
    {
        var utcNow = DateTimeHelper.UtcNow();
        if (PaymentMethods.RequiresOnlinePayment(order.PaymentMethod)
            && !OrderStatusHelper.IsPaid(order)
            && order.PaymentExpireAt.HasValue
            && order.PaymentExpireAt.Value > utcNow)
        {
            _logger.LogInformation(
                "Skipped expiring order {OrderId} because payment deadline is still valid. PaymentExpireAt={PaymentExpireAt:o}, NowUsedForCompare={NowUsedForCompare:o}, DifferenceSeconds={DifferenceSeconds}",
                order.Id,
                order.PaymentExpireAt,
                utcNow,
                (order.PaymentExpireAt.Value - utcNow).TotalSeconds);
            return;
        }

        if (order.Status != OrderStatus.Expired && order.Status != OrderStatus.Cancelled)
        {
            await ReleaseReservedStockAsync(order, cancellationToken);
        }

        LogExpiration(order, utcNow, order.Status, PaymentStatuses.Expired);
        order.Status = OrderStatus.Expired;
        order.PaymentStatus = PaymentStatuses.Expired;
    }

    public async Task MarkExpiredByAdminAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (order.Status != OrderStatus.Expired && order.Status != OrderStatus.Cancelled)
        {
            await ReleaseReservedStockAsync(order, cancellationToken);
        }

        LogExpiration(order, DateTimeHelper.UtcNow(), order.Status, PaymentStatuses.Expired);
        order.Status = OrderStatus.Expired;
        order.PaymentStatus = PaymentStatuses.Expired;
    }

    private bool RestorePendingPaymentIfDeadlineStillValid(Order order, DateTime utcNow)
    {
        if (PaymentMethods.IsCod(order.PaymentMethod)
            || OrderStatusHelper.IsPaid(order)
            || !order.PaymentExpireAt.HasValue
            || order.PaymentExpireAt.Value <= utcNow
            || (order.Status != OrderStatus.Expired
                && !string.Equals(order.PaymentStatus, PaymentStatuses.Expired, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        _logger.LogInformation(
            "Restoring order {OrderId} from premature expiration. CreatedAt={CreatedAt:o}, PaymentExpireAt={PaymentExpireAt:o}, NowUsedForCompare={NowUsedForCompare:o}, DifferenceSeconds={DifferenceSeconds}, OldStatus={OldStatus}, OldPaymentStatus={OldPaymentStatus}",
            order.Id,
            order.CreatedAt,
            order.PaymentExpireAt,
            utcNow,
            (order.PaymentExpireAt.Value - utcNow).TotalSeconds,
            order.Status,
            order.PaymentStatus);

        order.Status = OrderStatus.PendingPayment;
        order.PaymentStatus = PaymentStatuses.Pending;
        return true;
    }

    private void LogExpiration(Order order, DateTime utcNow, OrderStatus oldStatus, string newPaymentStatus)
    {
        _logger.LogInformation(
            "Expiring order {OrderId}. CreatedAt={CreatedAt:o}, PaymentExpireAt={PaymentExpireAt:o}, NowUsedForCompare={NowUsedForCompare:o}, DifferenceSeconds={DifferenceSeconds}, OldStatus={OldStatus}, NewStatus={NewStatus}",
            order.Id,
            order.CreatedAt,
            order.PaymentExpireAt,
            utcNow,
            order.PaymentExpireAt.HasValue ? (order.PaymentExpireAt.Value - utcNow).TotalSeconds : (double?)null,
            oldStatus,
            newPaymentStatus);
    }

    private static void EnsurePendingPaymentDeadline(Order order, DateTime utcNow)
    {
        if (order.PaymentExpireAt.HasValue)
        {
            return;
        }

        var candidateExpireAt = order.CreatedAt == default
            ? utcNow.Add(OrderStatusHelper.PendingPaymentTimeToLive)
            : order.CreatedAt.Add(OrderStatusHelper.PendingPaymentTimeToLive);

        order.PaymentExpireAt = candidateExpireAt;
    }

    private async Task ReleaseReservedStockAsync(Order order, CancellationToken cancellationToken)
    {
        var details = order.Details.Any()
            ? order.Details.ToList()
            : await _db.OrderDetails.Where(x => x.OrderId == order.Id).ToListAsync(cancellationToken);

        var productIds = details.Select(x => x.ProductId).Distinct().ToList();
        if (!productIds.Any())
        {
            return;
        }

        var products = await _db.Products
            .Where(x => productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        foreach (var detail in details)
        {
            if (!products.TryGetValue(detail.ProductId, out var product)) continue;
            product.StockQuantity += detail.Quantity;
            product.IsInStock = product.StockQuantity > 0;
        }
    }
}
