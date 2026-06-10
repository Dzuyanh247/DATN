using System.Security.Claims;
using Datn.PcStore.Data;
using Datn.PcStore.Helpers;
using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

public class WarrantyController : Controller
{
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxEvidenceSize = 5 * 1024 * 1024;
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public WarrantyController(ApplicationDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    [HttpGet("/Warranty")]
    [HttpGet("/Warranty/Check")]
    public async Task<IActionResult> Check(string? query)
    {
        var vm = new WarrantyCheckVm { Query = query?.Trim(), HasSearched = !string.IsNullOrWhiteSpace(query) };
        if (!vm.HasSearched) return View(vm);

        var normalized = vm.Query!;
        var numericText = new string(normalized.Where(char.IsDigit).ToArray());
        int.TryParse(numericText, out var numericId);
        var deterministicDetailId = ParseWarrantyDetailId(normalized);

        var orderDetailIdsByWarrantyCode = await _db.WarrantyRequests
            .Where(x => x.WarrantyCode == normalized && x.OrderDetailId.HasValue)
            .Select(x => x.OrderDetailId!.Value)
            .ToListAsync();

        var details = await _db.OrderDetails
            .AsNoTracking()
            .Include(x => x.Order)
            .Include(x => x.Product)
            .Where(x => x.Order != null &&
                (x.Order.ReceiverPhone == normalized ||
                 (numericId > 0 && x.OrderId == numericId) ||
                 orderDetailIdsByWarrantyCode.Contains(x.Id) ||
                 (deterministicDetailId.HasValue && x.Id == deterministicDetailId.Value)))
            .OrderByDescending(x => x.Order!.CreatedAt)
            .ToListAsync();

        var detailIds = details.Select(x => x.Id).ToList();
        var activeDetailIds = await _db.WarrantyRequests
            .Where(x => x.OrderDetailId.HasValue && detailIds.Contains(x.OrderDetailId.Value) &&
                        (x.Status == WarrantyStatuses.Pending || x.Status == WarrantyStatuses.Received || x.Status == WarrantyStatuses.Processing))
            .Select(x => x.OrderDetailId!.Value)
            .Distinct()
            .ToListAsync();

        var now = DateTimeHelper.UtcNow();
        vm.Products = details.Select(detail =>
        {
            var months = detail.WarrantyMonths > 0 ? detail.WarrantyMonths : detail.Product?.WarrantyMonths > 0 ? detail.Product.WarrantyMonths : 12;
            var purchaseDate = detail.Order!.CreatedAt;
            return new WarrantyProductVm
            {
                OrderId = detail.OrderId,
                OrderDetailId = detail.Id,
                ProductName = detail.ProductName,
                ProductImage = detail.ProductImage,
                WarrantyCode = BuildWarrantyCode(detail.OrderId, detail.Id),
                PurchaseDate = purchaseDate,
                WarrantyMonths = months,
                ExpiresAt = WarrantyPolicy.ExpiresAt(purchaseDate, months),
                IsEligibleOrder = WarrantyPolicy.IsOrderEligible(detail.Order),
                IsInWarranty = WarrantyPolicy.IsInWarranty(purchaseDate, months, now),
                HasActiveRequest = activeDetailIds.Contains(detail.Id)
            };
        }).ToList();

        return View(vm);
    }

    [HttpGet("/Warranty/Create")]
    public async Task<IActionResult> Create(int orderDetailId, string? phone)
    {
        var detail = await LoadOrderDetailAsync(orderDetailId);
        if (detail == null) return NotFound();
        if (!CanAccessOrder(detail.Order!, phone)) return Forbid();

        var validationRedirect = await ValidateCanCreateAsync(detail);
        if (validationRedirect != null) return validationRedirect;

        return View(BuildCreateVm(detail));
    }

    [HttpPost("/Warranty/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WarrantyCreateVm vm)
    {
        var detail = await LoadOrderDetailAsync(vm.OrderDetailId);
        if (detail == null) return NotFound();
        if (!CanAccessOrder(detail.Order!, vm.Phone)) return Forbid();

        var validationRedirect = await ValidateCanCreateAsync(detail);
        if (validationRedirect != null) return validationRedirect;

        if (vm.EvidenceImage is { Length: > 0 })
        {
            var extension = Path.GetExtension(vm.EvidenceImage.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(extension))
                ModelState.AddModelError(nameof(vm.EvidenceImage), "Chỉ chấp nhận ảnh JPG, PNG hoặc WEBP.");
            if (vm.EvidenceImage.Length > MaxEvidenceSize)
                ModelState.AddModelError(nameof(vm.EvidenceImage), "Ảnh minh chứng không được vượt quá 5 MB.");
        }

        if (!ModelState.IsValid)
        {
            CopyProductData(vm, detail);
            return View(vm);
        }

        string? evidencePath = null;
        if (vm.EvidenceImage is { Length: > 0 })
        {
            var extension = Path.GetExtension(vm.EvidenceImage.FileName).ToLowerInvariant();
            var fileName = $"warranty-{Guid.NewGuid():N}{extension}";
            var directory = Path.Combine(_environment.WebRootPath, "uploads", "warranty");
            Directory.CreateDirectory(directory);
            await using var stream = System.IO.File.Create(Path.Combine(directory, fileName));
            await vm.EvidenceImage.CopyToAsync(stream);
            evidencePath = $"/uploads/warranty/{fileName}";
        }

        var months = GetWarrantyMonths(detail);
        var request = new WarrantyRequest
        {
            OrderId = detail.OrderId,
            OrderDetailId = detail.Id,
            ProductId = detail.ProductId,
            UserId = CurrentUserId(),
            CustomerName = vm.CustomerName.Trim(),
            Phone = vm.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email.Trim(),
            ProductName = detail.ProductName,
            WarrantyCode = BuildWarrantyCode(detail.OrderId, detail.Id),
            PurchaseDate = detail.Order!.CreatedAt,
            WarrantyMonths = months,
            IssueTitle = vm.IssueTitle.Trim(),
            IssueDescription = vm.IssueDescription.Trim(),
            EvidencePath = evidencePath,
            Status = WarrantyStatuses.Pending
        };

        _db.WarrantyRequests.Add(request);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Đã gửi yêu cầu bảo hành {request.WarrantyCode}. Shop sẽ sớm liên hệ với bạn.";
        return RedirectToAction(nameof(Detail), new { id = request.Id, phone = request.UserId.HasValue ? null : request.Phone });
    }

    [HttpGet("/Warranty/MyRequests")]
    public async Task<IActionResult> MyRequests(string? phone)
    {
        var userId = CurrentUserId();
        var normalizedPhone = phone?.Trim();
        var vm = new WarrantyMyRequestsVm { Phone = normalizedPhone, RequiresPhone = !userId.HasValue };

        if (userId.HasValue || !string.IsNullOrWhiteSpace(normalizedPhone))
        {
            vm.Requests = await _db.WarrantyRequests.AsNoTracking()
                .Include(x => x.Product)
                .Where(x => userId.HasValue ? x.UserId == userId : x.Phone == normalizedPhone)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }
        return View(vm);
    }

    [HttpGet("/Warranty/Detail/{id:int}")]
    public async Task<IActionResult> Detail(int id, string? phone)
    {
        var request = await _db.WarrantyRequests.AsNoTracking()
            .Include(x => x.Order)
            .Include(x => x.OrderDetail)
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (request == null) return NotFound();
        if (!CanAccessRequest(request, phone)) return Forbid();
        return View(request);
    }

    private async Task<OrderDetail?> LoadOrderDetailAsync(int id) => await _db.OrderDetails
        .Include(x => x.Order).ThenInclude(x => x!.User)
        .Include(x => x.Product)
        .FirstOrDefaultAsync(x => x.Id == id);

    private async Task<IActionResult?> ValidateCanCreateAsync(OrderDetail detail)
    {
        if (!WarrantyPolicy.IsOrderEligible(detail.Order!))
        {
            TempData["ErrorMessage"] = "Sản phẩm chỉ có thể yêu cầu bảo hành sau khi đơn hàng đã thanh toán, giao hàng hoặc hoàn tất.";
            return RedirectToAction(nameof(Check), new { query = $"DH{detail.OrderId:D6}" });
        }
        if (!WarrantyPolicy.IsInWarranty(detail.Order!.CreatedAt, GetWarrantyMonths(detail), DateTimeHelper.UtcNow()))
        {
            TempData["InfoMessage"] = "Sản phẩm đã hết thời hạn bảo hành, vui lòng liên hệ shop để được hỗ trợ thêm.";
            return RedirectToAction(nameof(Check), new { query = $"DH{detail.OrderId:D6}" });
        }
        var duplicate = await _db.WarrantyRequests.AnyAsync(x => x.OrderDetailId == detail.Id &&
            (x.Status == WarrantyStatuses.Pending || x.Status == WarrantyStatuses.Received || x.Status == WarrantyStatuses.Processing));
        if (duplicate)
        {
            TempData["InfoMessage"] = "Sản phẩm này đang có một yêu cầu bảo hành được xử lý. Vui lòng theo dõi yêu cầu hiện tại.";
            return RedirectToAction(nameof(MyRequests), new { phone = detail.Order.ReceiverPhone });
        }
        return null;
    }

    private WarrantyCreateVm BuildCreateVm(OrderDetail detail)
    {
        var vm = new WarrantyCreateVm
        {
            CustomerName = detail.Order!.User?.FullName ?? detail.Order.ReceiverName,
            Phone = detail.Order.User?.Phone ?? detail.Order.ReceiverPhone,
            Email = detail.Order.User?.Email ?? detail.Order.CustomerEmail
        };
        CopyProductData(vm, detail);
        return vm;
    }

    private static void CopyProductData(WarrantyCreateVm vm, OrderDetail detail)
    {
        var months = GetWarrantyMonths(detail);
        vm.OrderDetailId = detail.Id;
        vm.OrderId = detail.OrderId;
        vm.OrderCode = $"DH{detail.OrderId:D6}";
        vm.ProductName = detail.ProductName;
        vm.ProductImage = detail.ProductImage;
        vm.WarrantyCode = BuildWarrantyCode(detail.OrderId, detail.Id);
        vm.PurchaseDate = detail.Order!.CreatedAt;
        vm.WarrantyMonths = months;
        vm.ExpiresAt = WarrantyPolicy.ExpiresAt(detail.Order.CreatedAt, months);
    }

    private bool CanAccessOrder(Order order, string? phone = null)
    {
        if (User.IsInRole("Admin")) return true;
        var userId = CurrentUserId();
        if (order.UserId.HasValue) return userId.HasValue && order.UserId == userId;
        return HttpContext.Session.GetInt32("LastOrderId") == order.Id ||
               (!string.IsNullOrWhiteSpace(phone) && order.ReceiverPhone == phone.Trim());
    }

    private bool CanAccessRequest(WarrantyRequest request, string? phone)
    {
        if (User.IsInRole("Admin")) return true;
        var userId = CurrentUserId();
        if (request.UserId.HasValue) return userId.HasValue && request.UserId == userId;
        return !string.IsNullOrWhiteSpace(phone) && request.Phone == phone.Trim();
    }

    private int? CurrentUserId() => User.Identity?.IsAuthenticated == true &&
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static int GetWarrantyMonths(OrderDetail detail) =>
        detail.WarrantyMonths > 0 ? detail.WarrantyMonths : detail.Product?.WarrantyMonths > 0 ? detail.Product.WarrantyMonths : 12;

    private static string BuildWarrantyCode(int orderId, int detailId) => $"BH-DH{orderId:D6}-CT{detailId:D6}";

    private static int? ParseWarrantyDetailId(string value)
    {
        var markerIndex = value.LastIndexOf("-CT", StringComparison.OrdinalIgnoreCase);
        return markerIndex >= 0 && int.TryParse(value[(markerIndex + 3)..], out var id) ? id : null;
    }
}
