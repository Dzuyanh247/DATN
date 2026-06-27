using Datn.PcStore.Data;
using Datn.PcStore.Helpers;
using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin,Staff,SupportStaff")]
public class AdminReportsController : Controller
{
    private const int PageSize = 12;
    private readonly ApplicationDbContext _db;

    public AdminReportsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Revenue(string? period = "today", int page = 1, CancellationToken cancellationToken = default)
    {
        period = string.Equals(period, "this-month", StringComparison.OrdinalIgnoreCase) ? "this-month" : "today";
        page = Math.Max(1, page);

        var vietnamNow = DateTime.UtcNow.AddHours(7);
        var localStart = period == "this-month" ? new DateTime(vietnamNow.Year, vietnamNow.Month, 1) : vietnamNow.Date;
        var localEnd = period == "this-month" ? localStart.AddMonths(1) : localStart.AddDays(1);
        var startUtc = DateTime.SpecifyKind(localStart.AddHours(-7), DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(localEnd.AddHours(-7), DateTimeKind.Utc);

        var paidOrders = await _db.Orders.AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Details)
            .Where(o => (o.PaidAt ?? o.CreatedAt) >= startUtc && (o.PaidAt ?? o.CreatedAt) < endUtc)
            .Where(o => o.Status != OrderStatus.Cancelled
                && o.Status != OrderStatus.Expired
                && o.PaymentStatus != PaymentStatuses.Refunded
                && ((o.Status == OrderStatus.Completed && o.PaymentStatus != PaymentStatuses.Refunded)
                    || (o.Status != OrderStatus.Expired && o.PaymentStatus == PaymentStatuses.Paid)))
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var ordered = paidOrders.OrderByDescending(o => o.PaidAt ?? o.CreatedAt).ToList();
        var totalItems = ordered.Count;
        var ordersPage = ordered.Skip((page - 1) * PageSize).Take(PageSize).ToList();

        var chartPoints = period == "this-month"
            ? Enumerable.Range(0, (localEnd - localStart).Days).Select(offset => BuildChartPoint(ordered, localStart.AddDays(offset), localStart.AddDays(offset + 1), "dd/MM")).ToList()
            : Enumerable.Range(0, 24).Select(hour => BuildChartPoint(ordered, localStart.AddHours(hour), localStart.AddHours(hour + 1), "HH:00")).ToList();

        var vm = new AdminRevenueReportVm
        {
            Period = period,
            PeriodLabel = period == "this-month" ? "Tháng này" : "Hôm nay",
            FromDate = localStart,
            ToDate = period == "this-month" ? localEnd.AddDays(-1) : localStart,
            ProductRevenue = ordered.Sum(o => o.SubtotalAmount > 0 ? o.SubtotalAmount : o.Details.Sum(d => d.TotalPrice > 0 ? d.TotalPrice : d.Quantity * d.UnitPrice)),
            ShippingFee = ordered.Sum(o => o.ShippingFee),
            DiscountAmount = ordered.Sum(o => o.DiscountAmount > 0 ? o.DiscountAmount : o.VoucherDiscountAmount),
            RefundAmount = 0,
            CustomerPaidCount = ordered.Select(o => o.UserId?.ToString() ?? FirstNonEmpty(o.ReceiverPhone, o.CustomerEmail, o.ReceiverName, $"order-{o.Id}")).Distinct().Count(),
            OrderCount = totalItems,
            TotalRevenue = ordered.Sum(o => o.TotalAmount),
            ChartPoints = chartPoints,
            TopProducts = ordered.SelectMany(o => o.Details).GroupBy(d => d.ProductName ?? d.Product?.Name ?? "Sản phẩm không xác định")
                .Select(g => new AdminTopProductRevenueVm { Name = g.Key, Quantity = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.TotalPrice > 0 ? x.TotalPrice : x.Quantity * x.UnitPrice) })
                .OrderByDescending(x => x.Quantity).ThenByDescending(x => x.Revenue).Take(10).ToList(),
            PaymentMethods = ordered.GroupBy(o => string.IsNullOrWhiteSpace(o.PaymentMethod) ? "UNKNOWN" : o.PaymentMethod)
                .Select(g => new AdminPaymentMethodRevenueVm { Method = g.Key!, OrderCount = g.Count(), Revenue = g.Sum(o => o.TotalAmount) }).OrderByDescending(x => x.Revenue).ToList(),
            OrderStatuses = ordered.GroupBy(o => o.Status).Select(g => new AdminOrderStatusRevenueVm { Status = g.Key, OrderCount = g.Count(), Revenue = g.Sum(o => o.TotalAmount) }).OrderByDescending(x => x.Revenue).ToList(),
            Orders = ordersPage.Select(o => new AdminRevenueOrderRowVm
            {
                Id = o.Id,
                CustomerName = FirstNonEmpty(o.ReceiverName, o.User?.FullName, "Khách hàng"),
                ProductRevenue = o.SubtotalAmount > 0 ? o.SubtotalAmount : o.Details.Sum(d => d.TotalPrice > 0 ? d.TotalPrice : d.Quantity * d.UnitPrice),
                ShippingFee = o.ShippingFee,
                DiscountAmount = o.DiscountAmount > 0 ? o.DiscountAmount : o.VoucherDiscountAmount,
                TotalAmount = o.TotalAmount,
                PaymentMethod = o.PaymentMethod ?? string.Empty,
                PaymentStatus = OrderStatusHelper.NormalizePaymentStatus(o.Status, o.PaymentStatus),
                Status = o.Status,
                RevenueAt = o.PaidAt ?? o.CreatedAt
            }).ToList(),
            Page = page,
            PageSize = PageSize,
            TotalItems = totalItems
        };

        return View(vm);
    }

    private static AdminRevenueChartPointVm BuildChartPoint(IEnumerable<Order> orders, DateTime localStart, DateTime localEnd, string labelFormat)
    {
        var startUtc = DateTime.SpecifyKind(localStart.AddHours(-7), DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(localEnd.AddHours(-7), DateTimeKind.Utc);
        var items = orders.Where(o => (o.PaidAt ?? o.CreatedAt) >= startUtc && (o.PaidAt ?? o.CreatedAt) < endUtc).ToList();
        return new AdminRevenueChartPointVm { Label = localStart.ToString(labelFormat), Revenue = items.Sum(o => o.TotalAmount), OrderCount = items.Count };
    }

    private static string FirstNonEmpty(params string?[] values) => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
}
