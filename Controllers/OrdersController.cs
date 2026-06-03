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
    private static readonly TimeSpan PendingPaymentTtl = TimeSpan.FromHours(2);

    private static bool IsOrderExpired(Order order, DateTime now)
        => IsPendingPaymentOrder(order) && order.PaymentExpireAt.HasValue && order.PaymentExpireAt.Value <= now;
    private readonly ApplicationDbContext _db;
    private readonly ICartService _cartService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(ApplicationDbContext db, ICartService cartService, ILogger<OrdersController> logger)
    {
        _db = db;
        _cartService = cartService;
        _logger = logger;
    }

    private static string ToOrderCode(int orderId) => $"DH{orderId:D6}";

    private static bool IsPendingPaymentOrder(Order order)
        => order.Status == OrderStatus.PendingPayment
           && order.PaymentMethod == "BANK_TRANSFER"
           && order.PaymentStatus == "WAITING_PAYMENT";

    private async Task<bool> ExpireOrderIfNeededAsync(Order order)
    {
        var now = DateTime.UtcNow;
        if (!IsOrderExpired(order, now))
        {
            return false;
        }

        order.Status = OrderStatus.Expired;
        order.PaymentStatus = "EXPIRED";

        var details = await _db.OrderDetails.Where(x => x.OrderId == order.Id).ToListAsync();
        foreach (var detail in details)
        {
            var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == detail.ProductId);
            if (product == null) continue;
            product.StockQuantity += detail.Quantity;
            product.IsInStock = product.StockQuantity > 0;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    [HttpGet("/Checkout")]
    public async Task<IActionResult> Checkout()
    {
        var vm = new CheckoutRequestVm();
        if (User.Identity?.IsAuthenticated == true)
        {
            var loggedInUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == loggedInUserId);
            if (user != null)
            {
                vm.CustomerName = user.FullName;
                vm.CustomerEmail = user.Email;
                vm.CustomerPhone = user.Phone;
            }
        }

        var userId = User.Identity?.IsAuthenticated == true ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) : (int?)null;
        ViewBag.Cart = await _cartService.GetCartAsync(userId);
        return View(vm);
    }

    [HttpPost("/Checkout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutRequestVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.CustomerName)) ModelState.AddModelError(nameof(vm.CustomerName), "Họ tên là bắt buộc.");
        if (string.IsNullOrWhiteSpace(vm.CustomerPhone) || !System.Text.RegularExpressions.Regex.IsMatch(vm.CustomerPhone, "^(0|\\+84)[0-9]{9,10}$")) ModelState.AddModelError(nameof(vm.CustomerPhone), "Số điện thoại không hợp lệ.");
        if (!string.IsNullOrWhiteSpace(vm.CustomerEmail) && !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(vm.CustomerEmail)) ModelState.AddModelError(nameof(vm.CustomerEmail), "Email không hợp lệ.");
        if (string.IsNullOrWhiteSpace(vm.AddressDetail))
            ModelState.AddModelError(nameof(vm.AddressDetail), "Địa chỉ cụ thể là bắt buộc.");

        if (string.IsNullOrWhiteSpace(vm.ProvinceName))
            vm.ProvinceName = vm.ManualProvince?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(vm.WardName))
            vm.WardName = vm.ManualWard?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(vm.ProvinceName))
            ModelState.AddModelError(nameof(vm.ProvinceName), "Tỉnh/Thành phố là bắt buộc.");
        if (string.IsNullOrWhiteSpace(vm.DistrictName))
            ModelState.AddModelError(nameof(vm.DistrictName), "Quận/Huyện là bắt buộc.");
        if (string.IsNullOrWhiteSpace(vm.WardName))
            ModelState.AddModelError(nameof(vm.WardName), "Phường/Xã là bắt buộc.");

        vm.FullAddress = string.IsNullOrWhiteSpace(vm.FullAddress)
            ? $"{vm.AddressDetail}, {vm.WardName}, {vm.DistrictName}, {vm.ProvinceName}, Vietnam"
            : vm.FullAddress;
        vm.ShippingFullAddress = string.IsNullOrWhiteSpace(vm.ShippingFullAddress) ? vm.FullAddress : vm.ShippingFullAddress;
        vm.CustomerAddress = vm.FullAddress; // Keep old flow/database field compatible

        var userId = User.Identity?.IsAuthenticated == true ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) : (int?)null;
        var cart = await _cartService.GetCartAsync(userId);
        if (!cart.Items.Any()) ModelState.AddModelError(string.Empty, "Không thể đặt hàng khi giỏ hàng trống.");
        if (vm.ShippingFee < 0 || string.IsNullOrWhiteSpace(vm.ShippingProvider))
            ModelState.AddModelError(string.Empty, "Vui lòng tính phí giao hàng hợp lệ trước khi đặt hàng.");
        if (vm.PaymentMethod != "COD" && vm.PaymentMethod != "BANK_TRANSFER")
            ModelState.AddModelError(nameof(vm.PaymentMethod), "Phương thức thanh toán không hợp lệ.");


        if (!ModelState.IsValid)
        {
            ViewBag.Cart = cart;
            return View(vm);
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

            if (vm.PaymentMethod == "BANK_TRANSFER")
            {
                Order? existingPending;
                if (userId.HasValue)
                {
                    existingPending = await _db.Orders
                        .Where(x => x.UserId == userId
                                    && x.PaymentMethod == "BANK_TRANSFER"
                                    && x.Status == OrderStatus.PendingPayment
                                    && x.PaymentStatus == "WAITING_PAYMENT")
                        .OrderByDescending(x => x.CreatedAt)
                        .FirstOrDefaultAsync();
                }
                else
                {
                    var pendingId = HttpContext.Session.GetInt32("LastPendingPaymentOrderId");
                    existingPending = pendingId.HasValue
                        ? await _db.Orders.FirstOrDefaultAsync(x => x.Id == pendingId.Value)
                        : null;
                }

                if (existingPending != null)
                {
                    var expired = await ExpireOrderIfNeededAsync(existingPending);
                    if (!expired)
                    {
                        TempData["InfoMessage"] = $"Bạn có đơn hàng chờ thanh toán {ToOrderCode(existingPending.Id)}. Vui lòng tiếp tục thanh toán trước khi tạo đơn mới.";
                        return RedirectToAction(nameof(BankTransfer), new { id = existingPending.Id });
                    }
                }
            }

            var discount = 0m;
            var paymentExpireAt = vm.PaymentMethod == "BANK_TRANSFER"
                ? DateTime.UtcNow.Add(PendingPaymentTtl)
                : (DateTime?)null;
            var order = new Order
            {
                UserId = userId,
                ReceiverName = vm.CustomerName,
                ReceiverPhone = vm.CustomerPhone,
                CustomerEmail = vm.CustomerEmail,
                ShippingAddress = vm.CustomerAddress,
                CustomerProvince = vm.ProvinceName,
                CustomerDistrict = vm.DistrictName,
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
                ShippingDistanceKm = vm.ShippingDistanceKm,
                ShippingDurationMinutes = vm.ShippingDurationMinutes,
                ShippingFee = vm.ShippingFee,
                ShippingProvider = vm.ShippingProvider,
                ShippingFormulaSnapshot = vm.ShippingFormulaSnapshot,
                TotalAmount = subtotal - discount + vm.ShippingFee,
                PaymentMethod = vm.PaymentMethod,
                PaymentStatus = vm.PaymentMethod == "BANK_TRANSFER" ? "WAITING_PAYMENT" : "UNPAID",
                Status = vm.PaymentMethod == "BANK_TRANSFER" ? OrderStatus.PendingPayment : OrderStatus.PendingConfirmation,
                PaymentExpireAt = paymentExpireAt,
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
            if (order.PaymentMethod == "BANK_TRANSFER")
            {
                order.TransferContent = $"DH{order.Id}";
                await _db.SaveChangesAsync();
                HttpContext.Session.SetInt32("LastPendingPaymentOrderId", order.Id);
            }
            HttpContext.Session.SetInt32("LastOrderId", order.Id);
            if (order.PaymentMethod != "BANK_TRANSFER")
            {
                await _cartService.ClearCartAsync(userId);
            }
            await tx.CommitAsync();
            TempData["SuccessMessage"] = "Đặt hàng thành công!";
            if (order.PaymentMethod == "BANK_TRANSFER")
            {
                return RedirectToAction(nameof(BankTransfer), new { id = order.Id });
            }
            return RedirectToAction(nameof(Success), new { id = order.Id });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Checkout failed for user {UserId}", userId);
            ModelState.AddModelError(string.Empty, "Không thể đặt hàng lúc này. Vui lòng kiểm tra tồn kho hoặc thử lại sau.");
            TempData["ErrorMessage"] = "Không thể đặt hàng, vui lòng kiểm tra thông tin và thử lại.";
            ViewBag.Cart = cart;
            return View(vm);
        }
    }

    public async Task<IActionResult> Success(int id)
    {
        var lastOrderId = HttpContext.Session.GetInt32("LastOrderId");
        if (lastOrderId != id) return RedirectToAction(nameof(Tracking), new { id });

        var order = await _db.Orders.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == id);
        if (order != null)
        {
            await ExpireOrderIfNeededAsync(order);
        }
        if (order == null) return NotFound();

        if (order.UserId.HasValue)
        {
            if (User.Identity?.IsAuthenticated != true) return Forbid();
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (order.UserId.Value != userId) return Forbid();
        }

        return View(order);
    }

    [HttpGet("/Orders/Quotation/{orderId:int}")]
    public async Task<IActionResult> Quotation(int orderId)
    {
        if (orderId <= 0) return NotFound();

        var order = await _db.Orders
            .Include(x => x.Details)
                .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == orderId);

        if (order == null) return NotFound();

        var isAdmin = User.IsInRole("Admin");
        if (order.UserId.HasValue)
        {
            if (User.Identity?.IsAuthenticated != true) return Forbid();

            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (order.UserId.Value != currentUserId && !isAdmin) return Forbid();
        }
        else if (!isAdmin && HttpContext.Session.GetInt32("LastOrderId") != order.Id)
        {
            return Forbid();
        }

        await ExpireOrderIfNeededAsync(order);

        var vm = new QuotationViewModel
        {
            OrderId = order.Id,
            OrderCode = ToOrderCode(order.Id),
            QuotationDate = DateTime.Now,
            CustomerName = order.ReceiverName,
            CustomerAddress = string.IsNullOrWhiteSpace(order.FullAddress) ? order.ShippingAddress : order.FullAddress,
            CustomerPhone = order.ReceiverPhone,
            CustomerEmail = order.CustomerEmail,
            TotalAmount = order.Details.Sum(x => x.TotalPrice),
            Items = order.Details.Select(x => new QuotationItemViewModel
            {
                ProductId = x.ProductId,
                ProductCode = string.IsNullOrWhiteSpace(x.Product?.ProductCode) ? $"SP{x.ProductId:D6}" : x.Product.ProductCode,
                ProductName = x.ProductName,
                ProductImage = x.ProductImage,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                Warranty = x.Warranty,
                LineTotal = x.TotalPrice
            }).ToList()
        };

        if (!vm.Items.Any()) return NotFound();

        return View(vm);
    }

    [HttpGet("/Order/Tracking/{id:int}")]
    public async Task<IActionResult> Tracking(int id)
    {
        var order = await _db.Orders.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == id);
        if (order == null) return NotFound();
        await ExpireOrderIfNeededAsync(order);

        if (order.UserId.HasValue)
        {
            if (User.Identity?.IsAuthenticated != true) return Forbid();
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (order.UserId.Value != userId) return Forbid();
            return View("Detail", order);
        }

        var canAccessBySession = HttpContext.Session.GetInt32("LastOrderId") == id;
        if (!canAccessBySession && string.IsNullOrWhiteSpace(Request.Query["phone"]))
        {
            TempData["TrackingError"] = "Vui lòng nhập số điện thoại để xem đơn hàng.";
            return RedirectToAction(nameof(TrackingLookup));
        }

        var phone = Request.Query["phone"].ToString();
        if (!canAccessBySession && !string.Equals(order.ReceiverPhone, phone, StringComparison.OrdinalIgnoreCase))
        {
            TempData["TrackingError"] = "Không tìm thấy đơn hàng.";
            return RedirectToAction(nameof(TrackingLookup));
        }

        return View("Detail", order);
    }

    [HttpGet("/Order/Lookup")]
    public IActionResult TrackingLookup() => View();

    [HttpPost("/Order/Lookup")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TrackingLookup(string orderCode, string phone)
    {
        if (string.IsNullOrWhiteSpace(orderCode) || string.IsNullOrWhiteSpace(phone))
        {
            TempData["TrackingError"] = "Vui lòng nhập mã đơn hàng và số điện thoại.";
            return View();
        }

        var digits = new string(orderCode.Where(char.IsDigit).ToArray());
        if (!int.TryParse(digits, out var orderId))
        {
            TempData["TrackingError"] = "Mã đơn hàng không hợp lệ.";
            return View();
        }

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.ReceiverPhone == phone);
        if (order == null)
        {
            TempData["TrackingError"] = "Không tìm thấy đơn hàng.";
            return View();
        }

        HttpContext.Session.SetInt32("LastOrderId", order.Id);
        return RedirectToAction(nameof(Tracking), new { id = order.Id, phone });
    }

    [Authorize]
    public async Task<IActionResult> MyOrders()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var orders = await _db.Orders.Include(o => o.Details).ThenInclude(d => d.Product).Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt).ToListAsync();
        foreach (var order in orders)
        {
            await ExpireOrderIfNeededAsync(order);
        }
        return View(orders);
    }

    [Authorize]
    public async Task<IActionResult> Detail(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _db.Orders.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (order == null) return NotFound();
        await ExpireOrderIfNeededAsync(order);
        return View(order);
    }

    [HttpPost("/Order/PayNow/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayNow(int id)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id);
        if (order == null) return NotFound();

        var isAdmin = User.IsInRole("Admin");
        if (order.UserId.HasValue)
        {
            if (User.Identity?.IsAuthenticated != true) return Forbid();
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (order.UserId.Value != userId && !isAdmin) return Forbid();
        }
        else if (!isAdmin && HttpContext.Session.GetInt32("LastOrderId") != order.Id)
        {
            TempData["ErrorMessage"] = "Bạn không có quyền thanh toán đơn hàng này.";
            return RedirectToAction(nameof(TrackingLookup));
        }

        await ExpireOrderIfNeededAsync(order);
        if (order.Status == OrderStatus.Expired)
        {
            TempData["ErrorMessage"] = "Đơn hàng đã hết hạn thanh toán.";
            return RedirectToAction(nameof(Tracking), new { id = order.Id });
        }

        if (!IsPendingPaymentOrder(order))
        {
            TempData["ErrorMessage"] = "Đơn hàng không ở trạng thái chờ thanh toán hợp lệ.";
            return RedirectToAction(nameof(Tracking), new { id = order.Id });
        }

        return RedirectToAction(nameof(BankTransfer), new { id = order.Id });
    }

    public async Task<IActionResult> BankTransfer(int id)
    {
        var order = await _db.Orders.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == id);
        if (order == null) return NotFound();
        await ExpireOrderIfNeededAsync(order);
        if (order.Status == OrderStatus.Expired)
        {
            TempData["ErrorMessage"] = "Đơn hàng đã hết hạn thanh toán (quá 2 giờ). Vui lòng đặt lại đơn hàng.";
            return RedirectToAction(nameof(Checkout));
        }
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmTransferred(int id)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id);
        if (order == null) return NotFound();
        if (order.PaymentMethod != "BANK_TRANSFER") return BadRequest();
        await ExpireOrderIfNeededAsync(order);
        if (order.Status == OrderStatus.Expired)
        {
            TempData["ErrorMessage"] = "Đơn hàng đã hết hạn thanh toán nên không thể xác nhận chuyển khoản.";
            return RedirectToAction(nameof(Checkout));
        }
        order.Status = OrderStatus.PendingConfirmation;
        order.PaymentStatus = "WAITING_CONFIRMATION";
        order.PaidAt ??= DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã ghi nhận xác nhận thanh toán của bạn.";
        return RedirectToAction(nameof(Detail), new { id });
    }

}
