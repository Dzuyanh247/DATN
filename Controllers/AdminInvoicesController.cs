using Datn.PcStore.Data;
using Datn.PcStore.Helpers;
using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Invoices")]
public class AdminInvoicesController : Controller
{
    private readonly ApplicationDbContext _db;
    public AdminInvoicesController(ApplicationDbContext db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, string? paymentStatus, CancellationToken cancellationToken)
    {
        var query = _db.Orders.AsNoTracking().Include(x => x.User).AsQueryable();
        search = search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var digits = new string(search.Where(char.IsDigit).ToArray());
            var hasId = int.TryParse(digits, out var id);
            query = query.Where(x => (hasId && x.Id == id) || x.ReceiverName.Contains(search)
                || x.ReceiverPhone.Contains(search) || (x.User != null && x.User.FullName.Contains(search)));
        }
        var orders = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        var rows = orders.Select(x => new AdminInvoiceRowVm
        {
            OrderId = x.Id,
            CustomerName = string.IsNullOrWhiteSpace(x.ReceiverName) ? x.User?.FullName ?? "Khách hàng" : x.ReceiverName,
            TotalAmount = x.TotalAmount,
            PaymentStatus = OrderStatusHelper.NormalizePaymentStatus(x.Status, x.PaymentStatus),
            OrderStatus = x.Status,
            CreatedAt = x.CreatedAt
        });
        if (!string.IsNullOrWhiteSpace(paymentStatus)) rows = rows.Where(x => x.PaymentStatus == paymentStatus);
        return View(new AdminInvoicesVm { Search = search, PaymentStatus = paymentStatus, Invoices = rows.ToList() });
    }
}
