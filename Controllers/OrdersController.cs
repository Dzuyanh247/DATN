using System.Security.Claims;
using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Datn.PcStore.Services;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

public class OrdersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICartService _cartService;

    public OrdersController(ApplicationDbContext db, ICartService cartService) { _db = db; _cartService = cartService; }

    [HttpGet("/Checkout")]
    public IActionResult Checkout()
    {
        var vm = new CheckoutRequestVm();
        if (User.Identity?.IsAuthenticated == true)
        {
            vm.CustomerName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        }

        return View(vm);
    }

    [HttpPost("/Checkout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutRequestVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.CustomerName)) ModelState.AddModelError(nameof(vm.CustomerName), "Họ tên là bắt buộc.");
        if (string.IsNullOrWhiteSpace(vm.CustomerPhone) || !System.Text.RegularExpressions.Regex.IsMatch(vm.CustomerPhone, "^(0|\\+84)[0-9]{9,10}$")) ModelState.AddModelError(nameof(vm.CustomerPhone), "Số điện thoại không hợp lệ.");
        if (!string.IsNullOrWhiteSpace(vm.CustomerEmail) && !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(vm.CustomerEmail)) ModelState.AddModelError(nameof(vm.CustomerEmail), "Email không hợp lệ.");
        if (string.IsNullOrWhiteSpace(vm.ProvinceCode) || string.IsNullOrWhiteSpace(vm.ProvinceName))
            ModelState.AddModelError(nameof(vm.ProvinceCode), "Tỉnh/Thành phố là bắt buộc.");
        if (string.IsNullOrWhiteSpace(vm.WardCode) || string.IsNullOrWhiteSpace(vm.WardName))
            ModelState.AddModelError(nameof(vm.WardCode), "Phường/Xã là bắt buộc.");
        if (string.IsNullOrWhiteSpace(vm.AddressDetail))
            ModelState.AddModelError(nameof(vm.AddressDetail), "Địa chỉ cụ thể là bắt buộc.");

        vm.FullAddress = string.IsNullOrWhiteSpace(vm.FullAddress)
            ? $"{vm.AddressDetail}, {vm.WardName}, {vm.ProvinceName}"
            : vm.FullAddress;
        vm.CustomerAddress = vm.FullAddress; // Keep old flow/database field compatible

        var userId = User.Identity?.IsAuthenticated == true ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) : (int?)null;
        var cart = await _cartService.GetCartAsync(userId);
        if (!cart.Items.Any()) ModelState.AddModelError(string.Empty, "Không thể đặt hàng khi giỏ hàng trống.");
        if (!ModelState.IsValid)
        {
            TempData["CartError"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(x => !string.IsNullOrWhiteSpace(x)));
            return RedirectToAction("Index", "Cart");
        }

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            decimal subtotal = 0;
            var detailRows = new List<OrderDetail>();
            foreach (var item in cart.Items)
            {
                var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == item.ProductId && x.IsActive);
                if (product == null) throw new Exception($"Sản phẩm #{item.ProductId} không tồn tại.");
                if (product.StockQuantity < item.Quantity) throw new Exception($"Sản phẩm {product.Name} không đủ tồn kho.");
                var unitPrice = product.DiscountPrice ?? product.SalePrice ?? product.Price;
                var lineTotal = unitPrice * item.Quantity;
                subtotal += lineTotal;
                detailRows.Add(new OrderDetail { ProductId = product.Id, Quantity = item.Quantity, UnitPrice = unitPrice, ProductName = product.Name, ProductImage = product.ThumbnailImage, Warranty = product.WarrantyDuration, TotalPrice = lineTotal });
            }

            var discount = 0m;
            var order = new Order
            {
                UserId = userId,
                ReceiverName = vm.CustomerName,
                ReceiverPhone = vm.CustomerPhone,
                CustomerEmail = vm.CustomerEmail,
                ShippingAddress = vm.CustomerAddress,
                CustomerProvince = vm.ProvinceName,
                CustomerDistrict = vm.WardName,
                ProvinceCode = vm.ProvinceCode,
                ProvinceName = vm.ProvinceName,
                WardCode = vm.WardCode,
                WardName = vm.WardName,
                AddressDetail = vm.AddressDetail,
                FullAddress = vm.FullAddress,
                Note = vm.Note,
                VoucherCode = vm.VoucherCode,
                SubtotalAmount = subtotal,
                DiscountAmount = discount,
                TotalAmount = subtotal - discount,
                PaymentMethod = "COD",
                Status = OrderStatus.Pending,
                Details = detailRows
            };

            foreach (var item in cart.Items)
            {
                var product = await _db.Products.FirstAsync(x => x.Id == item.ProductId);
                product.StockQuantity -= item.Quantity;
                if (product.StockQuantity < 0) throw new Exception($"Sản phẩm {product.Name} không đủ tồn kho.");
                product.IsInStock = product.StockQuantity > 0;
            }

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            await _cartService.ClearCartAsync(userId);
            await tx.CommitAsync();
            return RedirectToAction(nameof(Success), new { id = order.Id });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            TempData["CartError"] = ex.Message;
            return RedirectToAction("Index", "Cart");
        }
    }

    public async Task<IActionResult> Success(int id)
    {
        var order = await _db.Orders.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == id);
        if (order == null) return NotFound();

        if (order.UserId.HasValue)
        {
            if (User.Identity?.IsAuthenticated != true) return Forbid();
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (order.UserId.Value != userId) return Forbid();
        }

        return View(order);
    }

    [Authorize]
    public async Task<IActionResult> MyOrders()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var orders = await _db.Orders.Include(o => o.Details).ThenInclude(d => d.Product).Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt).ToListAsync();
        return View(orders);
    }

    [Authorize]
    public async Task<IActionResult> Detail(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _db.Orders.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (order == null) return NotFound();
        return View(order);
    }
}
