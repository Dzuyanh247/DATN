using System.Net;
using System.Security.Claims;
using System.Text;
using Datn.PcStore.Data;
using Datn.PcStore.Helpers;
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
    private readonly IOrderExpirationService _orderExpirationService;
    private readonly ILogger<OrdersController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IVoucherService _voucherService;
    private readonly IShippingService _shippingService;

    public OrdersController(
        ApplicationDbContext db,
        ICartService cartService,
        IOrderExpirationService orderExpirationService,
        ILogger<OrdersController> logger,
        IConfiguration configuration,
        IVoucherService voucherService,
        IShippingService shippingService)
    {
        _db = db;
        _cartService = cartService;
        _orderExpirationService = orderExpirationService;
        _logger = logger;
        _configuration = configuration;
        _voucherService = voucherService;
        _shippingService = shippingService;
    }

    private static string ToOrderCode(int orderId) => $"DH{orderId:D6}";

    private static bool IsBuyNowMode(string? mode) => string.Equals(mode, "buynow", StringComparison.OrdinalIgnoreCase);

    private static string ExcelCell(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private bool CanAccessOrderTracking(Order order, string? phone = null)
    {
        if (User.IsInRole("Admin"))
        {
            return true;
        }

        if (order.UserId.HasValue)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return order.UserId.Value == currentUserId;
        }

        if (HttpContext.Session.GetInt32("LastOrderId") == order.Id)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(phone)
               && string.Equals(order.ReceiverPhone, phone, StringComparison.OrdinalIgnoreCase);
    }


    [HttpGet("/Checkout")]
    public async Task<IActionResult> Checkout([FromQuery] string? mode = null)
    {
        var isBuyNow = IsBuyNowMode(mode);
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
        ViewBag.IsBuyNow = isBuyNow;
        ViewBag.CheckoutMode = isBuyNow ? "buynow" : string.Empty;
        ViewBag.Cart = isBuyNow ? await _cartService.GetBuyNowCartAsync() : await _cartService.GetCartAsync(userId);
        return View(vm);
    }

    [HttpPost("/Checkout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutRequestVm vm, [FromQuery] string? mode = null)
    {
        var isBuyNow = IsBuyNowMode(mode);
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
        if (string.IsNullOrWhiteSpace(vm.ProvinceCode) || !int.TryParse(vm.ProvinceCode, out var provinceId) || provinceId <= 0)
            ModelState.AddModelError(nameof(vm.ProvinceCode), "Mã Tỉnh/Thành phố GHN không hợp lệ.");
        if (string.IsNullOrWhiteSpace(vm.DistrictCode) || !int.TryParse(vm.DistrictCode, out var districtId) || districtId <= 0)
            ModelState.AddModelError(nameof(vm.DistrictCode), "Mã Quận/Huyện GHN không hợp lệ.");
        if (string.IsNullOrWhiteSpace(vm.WardCode))
            ModelState.AddModelError(nameof(vm.WardCode), "Mã Phường/Xã GHN không hợp lệ.");

        vm.FullAddress = string.IsNullOrWhiteSpace(vm.FullAddress)
            ? $"{vm.AddressDetail}, {vm.WardName}, {vm.DistrictName}, {vm.ProvinceName}, Vietnam"
            : vm.FullAddress;
        vm.ShippingFullAddress = string.IsNullOrWhiteSpace(vm.ShippingFullAddress) ? vm.FullAddress : vm.ShippingFullAddress;
        vm.CustomerAddress = vm.FullAddress; // Keep old flow/database field compatible

        var userId = User.Identity?.IsAuthenticated == true ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) : (int?)null;
        var cart = isBuyNow ? await _cartService.GetBuyNowCartAsync() : await _cartService.GetCartAsync(userId);
        if (!cart.Items.Any()) ModelState.AddModelError(string.Empty, isBuyNow ? "Không thể đặt hàng khi chưa chọn sản phẩm mua ngay." : "Không thể đặt hàng khi giỏ hàng trống.");
        if (vm.PaymentMethod != PaymentMethods.Cod && vm.PaymentMethod != PaymentMethods.BankTransfer)
            ModelState.AddModelError(nameof(vm.PaymentMethod), "Phương thức thanh toán không hợp lệ.");


        if (!ModelState.IsValid)
        {
            ViewBag.IsBuyNow = isBuyNow;
            ViewBag.CheckoutMode = isBuyNow ? "buynow" : string.Empty;
            ViewBag.Cart = cart;
            return View(vm);
        }

        try
        {
            var quantityForShipping = Math.Max(1, cart.Items.Sum(x => x.Quantity));
            var quote = await _shippingService.CalculateAsync(
                int.Parse(vm.DistrictCode),
                vm.WardCode,
                vm.ProvinceName,
                vm.DistrictName,
                vm.WardName,
                vm.AddressDetail,
                Math.Max(1000, quantityForShipping * 500),
                20,
                20,
                Math.Max(10, quantityForShipping * 2));

            vm.ShippingFee = quote.ShippingFee;
            vm.ShippingProvider = quote.Provider;
            vm.ShippingFormulaSnapshot = quote.FormulaSnapshot;
            vm.ShippingDistanceKm = (double)quote.DistanceKm;
            vm.ShippingDurationMinutes = quote.DurationMinutes;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Checkout shipping recalculation failed");
            ModelState.AddModelError(string.Empty, "Chưa tính được phí vận chuyển. Vui lòng kiểm tra lại địa chỉ và thử đặt hàng sau.");
            ViewBag.IsBuyNow = isBuyNow;
            ViewBag.CheckoutMode = isBuyNow ? "buynow" : string.Empty;
            ViewBag.Cart = cart;
            return View(vm);
        }

        if (vm.ShippingFee < 0 || string.IsNullOrWhiteSpace(vm.ShippingProvider))
        {
            ModelState.AddModelError(string.Empty, "Vui lòng tính phí giao hàng hợp lệ trước khi đặt hàng.");
            ViewBag.IsBuyNow = isBuyNow;
            ViewBag.CheckoutMode = isBuyNow ? "buynow" : string.Empty;
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
                detailRows.Add(new OrderDetail { ProductId = product.Id, Quantity = item.Quantity, UnitPrice = unitPrice, ProductName = product.Name, ProductImage = product.ThumbnailImage, Warranty = product.WarrantyDuration, WarrantyMonths = product.WarrantyMonths > 0 ? product.WarrantyMonths : 12, TotalPrice = lineTotal });
            }

            if (vm.PaymentMethod == PaymentMethods.BankTransfer)
            {
                Order? existingPending;
                if (userId.HasValue)
                {
                    existingPending = await _db.Orders
                        .Where(x => x.UserId == userId
                                    && x.PaymentMethod == PaymentMethods.BankTransfer
                                    && x.Status == OrderStatus.PendingPayment
                                    && (x.PaymentStatus == PaymentStatuses.Pending || x.PaymentStatus == PaymentStatuses.Unpaid))
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

                if (existingPending != null && !OrderStatusHelper.IsPendingPaymentOrder(existingPending))
                {
                    existingPending = null;
                }

                if (existingPending != null)
                {
                    var expired = await _orderExpirationService.ExpireOrderIfNeededAsync(existingPending);
                    if (!expired)
                    {
                        TempData["InfoMessage"] = $"Bạn có đơn hàng chờ thanh toán {ToOrderCode(existingPending.Id)}. Vui lòng tiếp tục thanh toán trước khi tạo đơn mới.";
                        return RedirectToAction(nameof(BankTransfer), new { id = existingPending.Id });
                    }
                }
            }

            var discount = 0m;
            Voucher? appliedVoucher = null;
            if (!string.IsNullOrWhiteSpace(vm.VoucherCode))
            {
                var voucherResult = await _voucherService.ValidateAsync(vm.VoucherCode, subtotal, vm.ShippingFee, userId);
                if (!voucherResult.Success)
                {
                    ModelState.AddModelError(nameof(vm.VoucherCode), voucherResult.Message);
                    ViewBag.IsBuyNow = isBuyNow;
                    ViewBag.CheckoutMode = isBuyNow ? "buynow" : string.Empty;
                    ViewBag.Cart = cart;
                    return View(vm);
                }
                appliedVoucher = voucherResult.Voucher;
                discount = voucherResult.DiscountAmount;
                vm.VoucherCode = appliedVoucher?.Code;
            }
            var finalTotal = Math.Max(subtotal + vm.ShippingFee - discount, 0m);
            var paymentExpireAt = vm.PaymentMethod == PaymentMethods.BankTransfer
                ? DateTimeHelper.UtcNow().Add(OrderStatusHelper.PendingPaymentTimeToLive)
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
                VoucherDiscountAmount = discount,
                FinalTotal = finalTotal,
                ShippingDistanceKm = vm.ShippingDistanceKm,
                ShippingDurationMinutes = vm.ShippingDurationMinutes,
                ShippingFee = vm.ShippingFee,
                ShippingProvider = vm.ShippingProvider,
                ShippingFormulaSnapshot = vm.ShippingFormulaSnapshot,
                TotalAmount = finalTotal,
                PaymentMethod = vm.PaymentMethod,
                PaymentStatus = vm.PaymentMethod == PaymentMethods.BankTransfer ? PaymentStatuses.Pending : PaymentStatuses.Unpaid,
                Status = vm.PaymentMethod == PaymentMethods.BankTransfer ? OrderStatus.PendingPayment : OrderStatus.PendingConfirmation,
                PaymentExpireAt = paymentExpireAt,
                CheckoutMode = isBuyNow ? "buynow" : "cart",
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
            if (appliedVoucher != null)
            {
                appliedVoucher.UsedCount += 1;
            }
            await _db.SaveChangesAsync();
            if (appliedVoucher != null)
            {
                _db.VoucherUsages.Add(new VoucherUsage
                {
                    VoucherId = appliedVoucher.Id,
                    UserId = userId,
                    OrderId = order.Id,
                    VoucherCode = appliedVoucher.Code,
                    DiscountAmount = discount
                });
                await _db.SaveChangesAsync();
            }
            if (order.PaymentMethod == PaymentMethods.BankTransfer)
            {
                order.TransferContent = $"DH{order.Id}";
                await _db.SaveChangesAsync();
                HttpContext.Session.SetInt32("LastPendingPaymentOrderId", order.Id);
            }
            HttpContext.Session.SetInt32("LastOrderId", order.Id);
            if (isBuyNow)
            {
                await _cartService.ClearBuyNowCartAsync();
            }
            else if (order.PaymentMethod != PaymentMethods.BankTransfer)
            {
                await _cartService.ClearCartAsync(userId);
            }
            await tx.CommitAsync();
            TempData["SuccessMessage"] = "Đặt hàng thành công!";
            if (order.PaymentMethod == PaymentMethods.BankTransfer)
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
            ViewBag.IsBuyNow = isBuyNow;
            ViewBag.CheckoutMode = isBuyNow ? "buynow" : string.Empty;
            ViewBag.Cart = cart;
            return View(vm);
        }
    }

    public async Task<IActionResult> Success(int id)
    {
        var lastOrderId = HttpContext.Session.GetInt32("LastOrderId");
        if (lastOrderId != id) return RedirectToAction(nameof(Tracking), new { id });

        var order = await _db.Orders
            .Include(x => x.Details)
                .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (order != null)
        {
            await _orderExpirationService.ExpireOrderIfNeededAsync(order);
        }
        if (order == null) return NotFound();

        if (order.UserId.HasValue)
        {
            if (User.Identity?.IsAuthenticated != true) return Forbid();
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (order.UserId.Value != userId) return Forbid();
        }

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Expired)
        {
            TempData["InfoMessage"] = order.Status == OrderStatus.Expired
                ? "Đơn hàng đã hết hạn thanh toán. Vui lòng kiểm tra chi tiết đơn hàng."
                : "Đơn hàng đã bị hủy. Vui lòng kiểm tra chi tiết đơn hàng.";
            return RedirectToAction(nameof(Tracking), new { id = order.Id });
        }

        return View(order);
    }

    [HttpGet("/Orders/ExportExcel/{orderId:int}")]
    public async Task<IActionResult> ExportExcel(int orderId)
    {
        if (orderId <= 0) return NotFound();

        var order = await _db.Orders
            .Include(x => x.Details)
                .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == orderId);

        if (order == null) return NotFound();
        if (!CanAccessOrderTracking(order)) return Forbid();

        await _orderExpirationService.ExpireOrderIfNeededAsync(order);

        var sb = new StringBuilder();
        sb.AppendLine("<html><head><meta charset=\"utf-8\" /></head><body><table border=\"1\">");
        sb.AppendLine("<thead><tr><th>Mã đơn hàng</th><th>Ngày đặt</th><th>Khách hàng</th><th>Số điện thoại</th><th>Email</th><th>Địa chỉ</th><th>Phương thức thanh toán</th><th>Mã sản phẩm</th><th>Sản phẩm</th><th>Số lượng</th><th>Đơn giá</th><th>Thành tiền</th><th>Bảo hành</th></tr></thead><tbody>");

        foreach (var item in order.Details)
        {
            var productCode = string.IsNullOrWhiteSpace(item.Product?.ProductCode)
                ? $"SP{item.ProductId:D6}"
                : item.Product.ProductCode;

            var createdAt = DateTimeHelper.FormatVietnam(order.CreatedAt, "dd/MM/yyyy HH:mm");
            var address = string.IsNullOrWhiteSpace(order.FullAddress) ? order.ShippingAddress : order.FullAddress;
            var paymentMethod = PaymentMethods.Label(order.PaymentMethod);

            sb.Append("<tr>");
            sb.Append($"<td>{ExcelCell(ToOrderCode(order.Id))}</td>");
            sb.Append($"<td>{ExcelCell(createdAt)}</td>");
            sb.Append($"<td>{ExcelCell(order.ReceiverName)}</td>");
            sb.Append($"<td>{ExcelCell(order.ReceiverPhone)}</td>");
            sb.Append($"<td>{ExcelCell(order.CustomerEmail)}</td>");
            sb.Append($"<td>{ExcelCell(address)}</td>");
            sb.Append($"<td>{ExcelCell(paymentMethod)}</td>");
            sb.Append($"<td>{ExcelCell(productCode)}</td>");
            sb.Append($"<td>{ExcelCell(item.ProductName)}</td>");
            sb.Append($"<td>{item.Quantity}</td>");
            sb.Append($"<td>{item.UnitPrice:N0}</td>");
            sb.Append($"<td>{item.TotalPrice:N0}</td>");
            sb.Append($"<td>{ExcelCell(item.Warranty)}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table></body></html>");

        var fileName = $"{ToOrderCode(order.Id)}-don-hang.xls";
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "application/vnd.ms-excel; charset=utf-8", fileName);
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

        await _orderExpirationService.ExpireOrderIfNeededAsync(order);
        var siteSettings = await _db.SiteSettings.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync();
        var shopLocation = await _db.ShopLocations.AsNoTracking()
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync();
        var configuredAddress = string.Join(", ", new[]
        {
            _configuration["ShopAddress:AddressDetail"],
            _configuration["ShopAddress:Ward"],
            _configuration["ShopAddress:District"],
            _configuration["ShopAddress:Province"]
        }.Where(value => !string.IsNullOrWhiteSpace(value)));

        var vm = new QuotationViewModel
        {
            OrderId = order.Id,
            OrderCode = ToOrderCode(order.Id),
            QuotationDate = DateTimeHelper.UtcNow(),
            OrderDate = order.CreatedAt,
            ShopName = string.IsNullOrWhiteSpace(siteSettings?.SiteName) ? "KKSHOP" : siteSettings.SiteName,
            ShopAddress = string.IsNullOrWhiteSpace(shopLocation?.Address) ? configuredAddress : shopLocation.Address,
            ShopPhone = _configuration["ShopContact:Phone"],
            ShopEmail = _configuration["EmailSettings:SenderEmail"],
            CustomerName = order.ReceiverName,
            CustomerAddress = string.IsNullOrWhiteSpace(order.FullAddress) ? order.ShippingAddress : order.FullAddress,
            CustomerPhone = order.ReceiverPhone,
            CustomerEmail = order.CustomerEmail,
            PaymentMethod = PaymentMethods.Label(order.PaymentMethod),
            PaymentStatus = OrderStatusHelper.PaymentLabel(order.PaymentStatus, order.Status),
            OrderStatus = OrderStatusHelper.Label(order.Status),
            IsCancelledOrExpired = order.Status is OrderStatus.Cancelled or OrderStatus.Expired,
            SubtotalAmount = order.SubtotalAmount > 0 ? order.SubtotalAmount : order.Details.Sum(x => x.TotalPrice),
            DiscountAmount = order.DiscountAmount,
            VoucherCode = order.VoucherCode,
            ShippingFee = order.ShippingFee,
            TotalAmount = order.TotalAmount,
            Note = order.Note,
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

        var phone = Request.Query["phone"].ToString();
        if (!CanAccessOrderTracking(order, phone))
        {
            if (order.UserId.HasValue)
            {
                return Forbid();
            }

            TempData["TrackingError"] = string.IsNullOrWhiteSpace(phone)
                ? "Vui lòng nhập số điện thoại để xem đơn hàng."
                : "Không tìm thấy đơn hàng.";
            return RedirectToAction(nameof(TrackingLookup));
        }

        await _orderExpirationService.ExpireOrderIfNeededAsync(order);
        ViewBag.PaymentRemainingSeconds = OrderStatusHelper.RemainingSeconds(order, DateTimeHelper.UtcNow());
        ViewBag.EnableTrackingStatusPolling = true;
        return View("Detail", order);
    }

    [HttpGet("/Order/TrackingStatus/{id:int}")]
    public async Task<IActionResult> TrackingStatus(int id, string? phone = null)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id);
        if (order == null) return NotFound();
        if (!CanAccessOrderTracking(order, phone)) return Forbid();

        await _orderExpirationService.ExpireOrderIfNeededAsync(order);
        var now = DateTimeHelper.UtcNow();

        return Json(new
        {
            orderId = order.Id,
            status = order.Status.ToString(),
            paymentStatus = order.PaymentStatus,
            paymentExpireAt = order.PaymentExpireAt,
            remainingSeconds = OrderStatusHelper.RemainingSeconds(order, now)
        });
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
    public async Task<IActionResult> MyOrders(string status = "all", string? keyword = null)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var orders = await _db.Orders
            .AsSplitQuery()
            .Include(o => o.Details)
                .ThenInclude(d => d.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        foreach (var order in orders)
        {
            await _orderExpirationService.ExpireOrderIfNeededAsync(order);
        }

        var reviewedOrderProducts = (await _db.ProductReviews
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new { x.OrderId, x.ProductId })
            .ToListAsync())
            .Select(x => (x.OrderId, x.ProductId))
            .ToHashSet();

        keyword = keyword?.Trim() ?? string.Empty;
        var normalizedStatus = status.Trim().ToLowerInvariant();
        var allowedStatuses = new[] { "all", "confirmation", "payment", "processing", "completed", "closed" };
        if (!allowedStatuses.Contains(normalizedStatus))
        {
            normalizedStatus = "all";
        }

        static bool MatchesStatus(Order order, string filter) => filter switch
        {
            "confirmation" => order.Status is OrderStatus.Pending or OrderStatus.PendingConfirmation,
            "payment" => order.Status == OrderStatus.PendingPayment,
            "processing" => order.Status is OrderStatus.Processing or OrderStatus.Delivering,
            "completed" => order.Status == OrderStatus.Completed,
            "closed" => order.Status is OrderStatus.Cancelled or OrderStatus.Expired,
            _ => true
        };

        var statusCounts = allowedStatuses.ToDictionary(
            filter => filter,
            filter => orders.Count(order => MatchesStatus(order, filter)));

        var filteredOrders = orders
            .Where(order => MatchesStatus(order, normalizedStatus))
            .Where(order => string.IsNullOrWhiteSpace(keyword)
                            || $"DH{order.Id:D6}".Contains(keyword, StringComparison.OrdinalIgnoreCase)
                            || order.Id.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .Select(order => new MyOrderRowViewModel
            {
                Order = order,
                ReviewedProductCount = order.Details
                    .Select(detail => detail.ProductId)
                    .Distinct()
                    .Count(productId => reviewedOrderProducts.Contains((order.Id, productId)))
            })
            .ToList();

        return View(new MyOrdersViewModel
        {
            Orders = filteredOrders,
            Status = normalizedStatus,
            Keyword = keyword,
            StatusCounts = statusCounts
        });
    }

    [Authorize]
    public async Task<IActionResult> Detail(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _db.Orders.Include(x => x.Details).ThenInclude(x => x.Product).FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (order == null) return NotFound();
        await _orderExpirationService.ExpireOrderIfNeededAsync(order);
        ViewBag.ReviewedProductIds = await _db.ProductReviews
            .Where(x => x.UserId == userId && x.OrderId == id)
            .Select(x => x.ProductId)
            .ToListAsync();
        ViewBag.PaymentRemainingSeconds = OrderStatusHelper.RemainingSeconds(order, DateTimeHelper.UtcNow());
        ViewBag.EnableTrackingStatusPolling = false;
        return View(order);
    }

    [HttpGet("/Order/Pay/{id:int}")]
    public Task<IActionResult> Pay(int id) => PayOrderAsync(id);

    [HttpPost("/Order/Pay/{id:int}")]
    [HttpPost("/Order/PayNow/{id:int}")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> PayNow(int id) => PayOrderAsync(id);

    private async Task<IActionResult> PayOrderAsync(int id)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id);
        if (order == null) return NotFound();

        if (!CanAccessOrderTracking(order))
        {
            TempData["ErrorMessage"] = "Bạn không có quyền thanh toán đơn hàng này.";
            return order.UserId.HasValue ? Forbid() : RedirectToAction(nameof(TrackingLookup));
        }

        await _orderExpirationService.ExpireOrderIfNeededAsync(order);
        if (OrderStatusHelper.IsExpiredPayment(order, DateTimeHelper.UtcNow()))
        {
            TempData["ErrorMessage"] = "Đơn hàng đã hết hạn thanh toán.";
            return RedirectToAction(nameof(Tracking), new { id = order.Id });
        }

        if (!OrderStatusHelper.CanPayNow(order, DateTimeHelper.UtcNow()))
        {
            TempData["ErrorMessage"] = "Đơn hàng không ở trạng thái chờ thanh toán hợp lệ.";
            return RedirectToAction(nameof(Tracking), new { id = order.Id });
        }

        if (!string.IsNullOrWhiteSpace(order.PaymentUrl))
        {
            return Redirect(order.PaymentUrl);
        }

        return RedirectToAction(nameof(BankTransfer), new { id = order.Id });
    }

    public async Task<IActionResult> BankTransfer(int id)
    {
        var order = await _db.Orders.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == id);
        if (order == null) return NotFound();
        if (!CanAccessOrderTracking(order)) return Forbid();
        await _orderExpirationService.ExpireOrderIfNeededAsync(order);
        if (OrderStatusHelper.IsExpiredPayment(order, DateTimeHelper.UtcNow()))
        {
            TempData["ErrorMessage"] = "Đơn hàng đã hết hạn thanh toán (quá 2 giờ). Vui lòng đặt lại đơn hàng.";
            return RedirectToAction(nameof(Checkout));
        }
        ViewBag.PaymentRemainingSeconds = OrderStatusHelper.RemainingSeconds(order, DateTimeHelper.UtcNow());
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmTransferred(int id)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id);
        if (order == null) return NotFound();
        if (!CanAccessOrderTracking(order)) return Forbid();
        if (order.PaymentMethod != PaymentMethods.BankTransfer) return BadRequest();
        await _orderExpirationService.ExpireOrderIfNeededAsync(order);
        if (OrderStatusHelper.IsExpiredPayment(order, DateTimeHelper.UtcNow()))
        {
            TempData["ErrorMessage"] = "Đơn hàng đã hết hạn thanh toán nên không thể xác nhận chuyển khoản.";
            return RedirectToAction(nameof(Checkout));
        }
        _orderExpirationService.MarkPaymentConfirmedByCustomer(order);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã ghi nhận xác nhận thanh toán của bạn.";
        return RedirectToAction(nameof(Tracking), new { id });
    }

}
