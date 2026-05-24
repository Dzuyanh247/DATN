using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
public class AdminOrdersController : Controller
{
    private readonly ApplicationDbContext _db;
    public AdminOrdersController(ApplicationDbContext db) => _db = db;

    private async Task ExpirePendingOrdersAsync(List<Order> orders)
    {
        var now = DateTime.UtcNow;
        var changed = false;
        foreach (var order in orders)
        {
            if (order.Status == OrderStatus.PendingPayment
                && order.PaymentMethod == "BANK_TRANSFER"
                && order.PaymentStatus == "WAITING_PAYMENT"
                && order.PaymentExpireAt.HasValue
                && order.PaymentExpireAt.Value <= now)
            {
                order.Status = OrderStatus.Expired;
                order.PaymentStatus = "EXPIRED";
                changed = true;
            }
        }

        if (changed)
        {
            await _db.SaveChangesAsync();
        }
    }

    public async Task<IActionResult> Index()
    {
        var orders = await _db.Orders.Include(o => o.User).OrderByDescending(o => o.CreatedAt).ToListAsync();
        await ExpirePendingOrdersAsync(orders);
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
        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();
        order.Status = status;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public async Task<IActionResult> ConfirmBankTransfer(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();
        if (order.PaymentMethod != "BANK_TRANSFER" || order.PaymentStatus != "WAITING_CONFIRMATION") return BadRequest();

        order.PaymentStatus = "PAID";
        order.Status = OrderStatus.Processing;
        order.PaidAt = DateTime.UtcNow;

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
