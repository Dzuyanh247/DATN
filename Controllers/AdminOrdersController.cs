using Datn.PcStore.Data;
using Datn.PcStore.Helpers;
using Datn.PcStore.Models;
using Datn.PcStore.Services;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
public class AdminOrdersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IOrderExpirationService _orderExpirationService;

    public AdminOrdersController(ApplicationDbContext db, IOrderExpirationService orderExpirationService)
    {
        _db = db;
        _orderExpirationService = orderExpirationService;
    }

    public async Task<IActionResult> Index(
        string? search,
        OrderStatus? status,
        string? paymentStatus,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        await _orderExpirationService.ExpirePendingPaymentOrdersAsync(cancellationToken);

        var query = _db.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Details)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p!.ProductImages)
            .AsSplitQuery()
            .AsQueryable();

        search = search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var orderIdSearch = search.StartsWith("DH", StringComparison.OrdinalIgnoreCase)
                ? search[2..]
                : search;
            var hasOrderId = int.TryParse(orderIdSearch, out var orderId);
            query = query.Where(o => (hasOrderId && o.Id == orderId)
                || o.ReceiverName.Contains(search)
                || o.ReceiverPhone.Contains(search)
                || (o.User != null && (o.User.FullName.Contains(search) || o.User.Phone.Contains(search))));
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(paymentStatus))
        {
            query = paymentStatus switch
            {
                PaymentStatuses.Paid => query.Where(o =>
                    (o.Status == OrderStatus.Completed && o.PaymentStatus != PaymentStatuses.Refunded)
                    || (o.Status != OrderStatus.Expired && o.PaymentStatus == PaymentStatuses.Paid)),
                PaymentStatuses.Expired => query.Where(o =>
                    o.Status == OrderStatus.Expired
                    || (o.Status != OrderStatus.Completed && o.PaymentStatus == PaymentStatuses.Expired)),
                PaymentStatuses.Pending => query.Where(o =>
                    o.Status == OrderStatus.PendingPayment
                    && (o.PaymentStatus == PaymentStatuses.Pending || o.PaymentStatus == PaymentStatuses.Unpaid)),
                PaymentStatuses.Failed => query.Where(o =>
                    o.PaymentStatus == PaymentStatuses.Failed
                    || (o.Status == OrderStatus.Cancelled
                        && o.PaymentStatus != PaymentStatuses.Paid
                        && o.PaymentStatus != PaymentStatuses.Refunded)),
                _ => query.Where(o => o.PaymentStatus == paymentStatus)
            };
        }

        // Ngày nhập trên giao diện là ngày Việt Nam (UTC+7), dữ liệu trong DB được lưu UTC.
        if (fromDate.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(fromDate.Value.Date.AddHours(-7), DateTimeKind.Utc);
            query = query.Where(o => o.CreatedAt >= fromUtc);
        }

        if (toDate.HasValue)
        {
            var toUtcExclusive = DateTime.SpecifyKind(toDate.Value.Date.AddDays(1).AddHours(-7), DateTimeKind.Utc);
            query = query.Where(o => o.CreatedAt < toUtcExclusive);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        var orderStates = await _db.Orders
            .AsNoTracking()
            .Select(o => new { o.Status, o.PaymentStatus })
            .ToListAsync(cancellationToken);
        var stats = new AdminOrderStatsVm
        {
            TotalOrders = orderStates.Count,
            PendingPaymentOrders = orderStates.Count(o =>
                o.Status == OrderStatus.PendingPayment
                && OrderStatusHelper.NormalizePaymentStatus(o.Status, o.PaymentStatus) == PaymentStatuses.Pending),
            PaidOrders = orderStates.Count(o => OrderStatusHelper.IsEffectivelyPaid(o.Status, o.PaymentStatus)),
            ExpiredPaymentOrders = orderStates.Count(o =>
                OrderStatusHelper.NormalizePaymentStatus(o.Status, o.PaymentStatus) == PaymentStatuses.Expired)
        };

        var model = new AdminOrdersIndexVm
        {
            Search = search,
            Status = status,
            PaymentStatus = paymentStatus,
            FromDate = fromDate,
            ToDate = toDate,
            Stats = stats,
            Orders = orders.Select(MapOrderRow).ToList()
        };

        return View(model);
    }

    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .Include(o => o.User)
            .Include(o => o.Details)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p!.ProductImages)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order == null) return NotFound();
        await _orderExpirationService.ExpireOrderIfNeededAsync(order, cancellationToken);
        return View(MapOrderDetail(order));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status, string? returnUrl)
    {
        if (!Enum.IsDefined(status)) return BadRequest();

        var order = await _db.Orders.Include(o => o.Details).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();

        switch (status)
        {
            case OrderStatus.PendingPayment:
                if (PaymentMethods.IsCod(order.PaymentMethod))
                {
                    order.PaymentMethod = PaymentMethods.BankTransfer;
                }
                _orderExpirationService.PreparePendingPayment(order, resetExpiredDeadline: true);
                break;
            case OrderStatus.PendingConfirmation:
            case OrderStatus.Pending:
                order.Status = OrderStatus.PendingConfirmation;
                order.PaymentExpireAt = null;
                if (PaymentMethods.RequiresOnlinePayment(order.PaymentMethod) && !OrderStatusHelper.IsPaid(order))
                {
                    order.PaymentStatus = PaymentStatuses.PendingConfirmation;
                }
                break;
            case OrderStatus.Processing:
                _orderExpirationService.MarkPaidByAdmin(order);
                break;
            case OrderStatus.Delivering:
                order.Status = OrderStatus.Delivering;
                order.PaymentExpireAt = null;
                if (PaymentMethods.RequiresOnlinePayment(order.PaymentMethod))
                {
                    order.PaymentStatus = PaymentStatuses.Paid;
                    order.PaidAt ??= DateTimeHelper.UtcNow();
                }
                break;
            case OrderStatus.Completed:
                order.Status = OrderStatus.Completed;
                order.PaymentStatus = PaymentStatuses.Paid;
                order.PaymentExpireAt = null;
                order.PaidAt ??= DateTimeHelper.UtcNow();
                break;
            case OrderStatus.Cancelled:
                await _orderExpirationService.MarkCancelledAsync(order);
                break;
            case OrderStatus.Expired:
                await _orderExpirationService.MarkExpiredByAdminAsync(order);
                break;
            default:
                order.Status = status;
                break;
        }

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn DH{order.Id:D6}.";
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmBankTransfer(int id)
    {
        var order = await _db.Orders.Include(o => o.Details).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();
        if (order.PaymentMethod != PaymentMethods.BankTransfer || order.PaymentStatus != PaymentStatuses.PendingConfirmation) return BadRequest();

        _orderExpirationService.MarkPaidByAdmin(order);

        if (order.UserId.HasValue && !string.Equals(order.CheckoutMode, "buynow", StringComparison.OrdinalIgnoreCase))
        {
            var cart = await _db.Carts.Include(x => x.Items).FirstOrDefaultAsync(x => x.UserId == order.UserId.Value);
            if (cart != null && cart.Items.Any())
            {
                _db.CartItems.RemoveRange(cart.Items);
            }
        }

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Đã xác nhận thanh toán đơn DH{order.Id:D6}.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    private static AdminOrderRowVm MapOrderRow(Order order) => new()
    {
        Id = order.Id,
        CustomerName = FirstNonEmpty(order.ReceiverName, order.User?.FullName, "Khách hàng"),
        CustomerPhone = FirstNonEmpty(order.ReceiverPhone, order.User?.Phone, "—"),
        TotalAmount = order.TotalAmount,
        Status = order.Status,
        PaymentStatus = OrderStatusHelper.NormalizePaymentStatus(order.Status, order.PaymentStatus),
        PaymentDeadline = order.Status == OrderStatus.PendingPayment ? order.PaymentExpireAt : null,
        CreatedAt = order.CreatedAt,
        Products = order.Details.OrderBy(d => d.Id).Select(MapProduct).ToList()
    };

    private static AdminOrderDetailVm MapOrderDetail(Order order) => new()
    {
        Id = order.Id,
        CustomerName = FirstNonEmpty(order.ReceiverName, order.User?.FullName, "Khách hàng"),
        CustomerPhone = FirstNonEmpty(order.ReceiverPhone, order.User?.Phone, "—"),
        CustomerEmail = FirstNonEmpty(order.CustomerEmail, order.User?.Email),
        ShippingAddress = FirstNonEmpty(order.FullAddress, order.ShippingAddress, "Chưa cập nhật"),
        Note = order.Note,
        PaymentMethod = order.PaymentMethod,
        PaymentStatus = OrderStatusHelper.NormalizePaymentStatus(order.Status, order.PaymentStatus),
        PaidAt = order.PaidAt,
        PaymentDeadline = order.Status == OrderStatus.PendingPayment ? order.PaymentExpireAt : null,
        Status = order.Status,
        CreatedAt = order.CreatedAt,
        SubtotalAmount = order.SubtotalAmount,
        DiscountAmount = order.DiscountAmount,
        VoucherCode = order.VoucherCode,
        ShippingFee = order.ShippingFee,
        TotalAmount = order.TotalAmount,
        Products = order.Details.OrderBy(d => d.Id).Select(MapProduct).ToList()
    };

    private static AdminOrderProductVm MapProduct(OrderDetail detail)
    {
        var currentProductImage = detail.Product?.ProductImages
            .OrderByDescending(image => image.IsPrimary)
            .ThenBy(image => image.SortOrder)
            .Select(image => image.ImageUrl)
            .FirstOrDefault();

        return new AdminOrderProductVm
        {
            Name = FirstNonEmpty(detail.ProductName, detail.Product?.Name, "Sản phẩm không còn tồn tại"),
            ImageUrl = FirstNonEmpty(detail.ProductImage, currentProductImage, detail.Product?.ThumbnailImage),
            Quantity = detail.Quantity,
            UnitPrice = detail.UnitPrice
        };
    }

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
}
