using Datn.PcStore.Data;
using Datn.PcStore.Helpers;
using Datn.PcStore.Models;
using Datn.PcStore.Services;
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

    public async Task<IActionResult> Index()
    {
        var orders = await _db.Orders.Include(o => o.User).OrderByDescending(o => o.CreatedAt).ToListAsync();
        foreach (var order in orders)
        {
            await _orderExpirationService.ExpireOrderIfNeededAsync(order);
        }
        return View(orders);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var order = await _db.Orders
            .Include(o => o.User)
            .Include(o => o.Details)
            .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();
        await _orderExpirationService.ExpireOrderIfNeededAsync(order);
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
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
                if (PaymentMethods.RequiresOnlinePayment(order.PaymentMethod))
                {
                    order.PaymentStatus = PaymentStatuses.Paid;
                    order.PaidAt ??= DateTime.UtcNow;
                }
                break;
            case OrderStatus.Completed:
                order.Status = OrderStatus.Completed;
                if (PaymentMethods.RequiresOnlinePayment(order.PaymentMethod))
                {
                    order.PaymentStatus = PaymentStatuses.Paid;
                    order.PaidAt ??= DateTime.UtcNow;
                }
                break;
            case OrderStatus.Cancelled:
                await _orderExpirationService.MarkCancelledAsync(order);
                break;
            case OrderStatus.Expired:
                await _orderExpirationService.ExpireOrderIfNeededAsync(order);
                if (order.Status != OrderStatus.Expired)
                {
                    await _orderExpirationService.MarkExpiredAsync(order);
                }
                break;
            default:
                order.Status = status;
                break;
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmBankTransfer(int id)
    {
        var order = await _db.Orders.Include(o => o.Details).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();
        if (order.PaymentMethod != PaymentMethods.BankTransfer || order.PaymentStatus != PaymentStatuses.PendingConfirmation) return BadRequest();

        _orderExpirationService.MarkPaidByAdmin(order);

        if (order.UserId.HasValue)
        {
            var cart = await _db.Carts.Include(x => x.Items).FirstOrDefaultAsync(x => x.UserId == order.UserId.Value);
            if (cart != null && cart.Items.Any())
            {
                _db.CartItems.RemoveRange(cart.Items);
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Detail), new { id });
    }
}
