using System.Security.Claims;
using Datn.PcStore.Data;
using Datn.PcStore.Helpers;
using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
public class AdminDashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    public AdminDashboardController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var vietnamNow = DateTime.UtcNow.AddHours(7);
        var todayUtc = DateTime.SpecifyKind(vietnamNow.Date.AddHours(-7), DateTimeKind.Utc);
        var tomorrowUtc = todayUtc.AddDays(1);
        var monthStartUtc = DateTime.SpecifyKind(new DateTime(vietnamNow.Year, vietnamNow.Month, 1).AddHours(-7), DateTimeKind.Utc);
        var chartStartUtc = todayUtc.AddDays(-6);

        var orders = await _db.Orders.AsNoTracking()
            .Where(x => x.CreatedAt >= chartStartUtc || x.PaidAt >= monthStartUtc)
            .Select(x => new { x.Id, x.Status, x.PaymentStatus, x.PaymentMethod, x.TotalAmount, x.CreatedAt, x.PaidAt })
            .ToListAsync(cancellationToken);
        var validPaidOrders = orders.Where(x => x.Status != OrderStatus.Cancelled
            && x.Status != OrderStatus.Expired
            && x.PaymentStatus != PaymentStatuses.Refunded
            && OrderStatusHelper.IsEffectivelyPaid(x.Status, x.PaymentStatus)).ToList();

        var recentOrders = await _db.Orders.AsNoTracking()
            .Include(x => x.User).Include(x => x.Details)
            .OrderByDescending(x => x.CreatedAt).Take(8).ToListAsync(cancellationToken);
        var attentionProducts = await _db.Products.AsNoTracking().Include(x => x.Category)
            .OrderBy(x => x.StockQuantity).ThenByDescending(x => x.UpdatedAt).Take(6).ToListAsync(cancellationToken);
        var allOrderStates = await _db.Orders.AsNoTracking()
            .Select(x => new { x.Status, x.PaymentStatus, x.PaymentMethod }).ToListAsync(cancellationToken);

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var adminName = int.TryParse(userIdValue, out var userId)
            ? await _db.Users.Where(x => x.Id == userId).Select(x => x.FullName).FirstOrDefaultAsync(cancellationToken)
            : null;

        var vm = new AdminDashboardVm
        {
            ProductCount = await _db.Products.CountAsync(cancellationToken),
            OrderCount = allOrderStates.Count,
            UserCount = await _db.Users.CountAsync(cancellationToken),
            WarrantyRequestCount = await _db.WarrantyRequests.CountAsync(cancellationToken),
            PendingOrderCount = allOrderStates.Count(x => x.Status is OrderStatus.Pending or OrderStatus.PendingConfirmation),
            PendingPaymentCount = allOrderStates.Count(x => x.Status == OrderStatus.PendingPayment),
            BankTransferConfirmationCount = allOrderStates.Count(x => x.PaymentMethod == PaymentMethods.BankTransfer && x.PaymentStatus == PaymentStatuses.PendingConfirmation),
            NewWarrantyCount = await _db.WarrantyRequests.CountAsync(x => x.Status == WarrantyStatuses.Pending, cancellationToken),
            UnreadSupportCount = await _db.ChatConversations.SumAsync(x => (int?)x.StaffUnreadCount, cancellationToken) ?? 0,
            LowStockCount = await _db.Products.CountAsync(x => x.IsActive && x.StockQuantity <= 5, cancellationToken),
            TodayRevenue = validPaidOrders.Where(x => (x.PaidAt ?? x.CreatedAt) >= todayUtc && (x.PaidAt ?? x.CreatedAt) < tomorrowUtc).Sum(x => x.TotalAmount),
            MonthRevenue = validPaidOrders.Where(x => (x.PaidAt ?? x.CreatedAt) >= monthStartUtc).Sum(x => x.TotalAmount),
            AdminDisplayName = string.IsNullOrWhiteSpace(adminName) ? User.Identity?.Name ?? "Quản trị viên" : adminName,
            RecentOrders = recentOrders.Select(x => new AdminDashboardOrderVm
            {
                Id = x.Id, CustomerName = string.IsNullOrWhiteSpace(x.ReceiverName) ? x.User?.FullName ?? "Khách hàng" : x.ReceiverName,
                TotalAmount = x.TotalAmount, Status = x.Status, PaymentStatus = OrderStatusHelper.NormalizePaymentStatus(x.Status, x.PaymentStatus),
                CreatedAt = x.CreatedAt, ProductImage = x.Details.OrderBy(d => d.Id).Select(d => d.ProductImage).FirstOrDefault()
            }).ToList(),
            AttentionProducts = attentionProducts.Select(x => new AdminDashboardProductVm
            {
                Id = x.Id, Name = x.Name ?? "Sản phẩm không xác định", ImageUrl = x.ThumbnailImage ?? "/images/no-image.png", CategoryName = x.Category?.Name ?? "Chưa phân loại",
                Price = x.DiscountPrice ?? x.SalePrice ?? x.Price, StockQuantity = x.StockQuantity, IsActive = x.IsActive, UpdatedAt = x.UpdatedAt
            }).ToList(),
            RevenueLastSevenDays = Enumerable.Range(0, 7).Select(offset =>
            {
                var localDate = vietnamNow.Date.AddDays(offset - 6);
                var startUtc = DateTime.SpecifyKind(localDate.AddHours(-7), DateTimeKind.Utc);
                var endUtc = startUtc.AddDays(1);
                var daily = validPaidOrders.Where(x => (x.PaidAt ?? x.CreatedAt) >= startUtc && (x.PaidAt ?? x.CreatedAt) < endUtc).ToList();
                return new AdminDashboardChartPointVm { Date = localDate, Revenue = daily.Sum(x => x.TotalAmount), OrderCount = daily.Count };
            }).ToList()
        };
        return View(vm);
    }
}
