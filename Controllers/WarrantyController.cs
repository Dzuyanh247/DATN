using System.Data;
using System.Security.Claims;
using System.Text.RegularExpressions;
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
        var deterministicDetailId = WarrantyCodeHelper.ParseWarrantyDetailId(normalized);

        var matchingRequests = await _db.WarrantyRequests
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.OrderDetail)
            .Where(x => x.Phone == normalized || x.WarrantyCode == normalized || x.RequestCode == normalized ||
                        (numericId > 0 && x.OrderId == numericId))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var requestDetailIds = matchingRequests
            .Where(x => x.OrderDetailId.HasValue)
            .Select(x => x.OrderDetailId!.Value)
            .Distinct()
            .ToList();

        var details = await _db.OrderDetails
            .AsNoTracking()
            .Include(x => x.Order).ThenInclude(x => x!.User)
            .Include(x => x.Product)
            .Where(x => x.Order != null &&
                (x.Order.ReceiverPhone == normalized ||
                 (numericId > 0 && x.OrderId == numericId) ||
                 requestDetailIds.Contains(x.Id) ||
                 (deterministicDetailId.HasValue && x.Id == deterministicDetailId.Value)))
            .OrderByDescending(x => x.Order!.CreatedAt)
            .ToListAsync();

        var detailIds = details.Select(x => x.Id).ToList();
        var relatedRequests = await _db.WarrantyRequests
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.OrderDetail)
            .Where(x => (x.OrderDetailId.HasValue && detailIds.Contains(x.OrderDetailId.Value)) ||
                        x.Phone == normalized || x.WarrantyCode == normalized || x.RequestCode == normalized ||
                        (numericId > 0 && x.OrderId == numericId))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        vm.Requests = relatedRequests
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        var activeDetailIds = vm.Requests
            .Where(x => x.OrderDetailId.HasValue && WarrantyStatuses.IsActive(x.Status))
            .Select(x => x.OrderDetailId!.Value)
            .Distinct()
            .ToHashSet();

        var now = DateTimeHelper.UtcNow();
        vm.Products = details.Select(detail => BuildWarrantyProductVm(detail, activeDetailIds.Contains(detail.Id), now)).ToList();

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

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var duplicateExists = await _db.WarrantyRequests.AnyAsync(x => x.OrderDetailId == detail.Id &&
                                                                      WarrantyStatuses.AllActive.Contains(x.Status));
        if (duplicateExists)
        {
            await transaction.RollbackAsync();
            DeleteEvidenceFile(evidencePath);
            TempData["InfoMessage"] = "Sản phẩm này đang có yêu cầu bảo hành được xử lý. Vui lòng theo dõi trạng thái bên dưới.";
            return RedirectToAction(nameof(Check), new { query = WarrantyCodeHelper.BuildWarrantyCode(detail.OrderId, detail.Id) });
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
            ProductName = detail.ProductName ?? string.Empty,
            RequestCode = $"TMP-{Guid.NewGuid():N}",
            WarrantyCode = WarrantyCodeHelper.BuildWarrantyCode(detail.OrderId, detail.Id),
            PurchaseDate = detail.Order!.CreatedAt,
            WarrantyMonths = months,
            IssueTitle = vm.IssueTitle.Trim(),
            IssueDescription = vm.IssueDescription.Trim(),
            EvidencePath = evidencePath,
            Status = WarrantyStatuses.Pending
        };

        _db.WarrantyRequests.Add(request);
        await _db.SaveChangesAsync();
        request.RequestCode = WarrantyCodeHelper.BuildRequestCode(request.Id);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = $"Đã gửi yêu cầu bảo hành {request.RequestCode}. Shop sẽ sớm liên hệ với bạn.";
        return RedirectToAction(nameof(Detail), new { id = request.Id, query = request.UserId.HasValue ? null : request.Phone });
    }

    [HttpGet("/Warranty/MyRequests")]
    public async Task<IActionResult> MyRequests(string? query, string? phone)
    {
        var userId = CurrentUserId();
        var lookup = (query ?? phone)?.Trim();
        var vm = new WarrantyMyRequestsVm
        {
            Query = lookup,
            RequiresLookup = !userId.HasValue,
            HasSearched = userId.HasValue || !string.IsNullOrWhiteSpace(lookup)
        };

        if (!vm.HasSearched) return View(vm);

        var requests = _db.WarrantyRequests.AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.OrderDetail)
            .AsQueryable();

        if (userId.HasValue)
        {
            requests = requests.Where(x => x.UserId == userId);
        }
        else
        {
            var digits = new string(lookup!.Where(char.IsDigit).ToArray());
            int.TryParse(digits, out var orderId);
            requests = requests.Where(x => x.Phone == lookup || x.WarrantyCode == lookup || x.RequestCode == lookup ||
                                           (orderId > 0 && x.OrderId == orderId));
        }

        vm.Requests = await requests.OrderByDescending(x => x.CreatedAt).ToListAsync();
        return View(vm);
    }

    [HttpGet("/Warranty/Detail/{id:int}")]
    public async Task<IActionResult> Detail(int id, string? query, string? phone)
    {
        var request = await _db.WarrantyRequests.AsNoTracking()
            .Include(x => x.Order)
            .Include(x => x.OrderDetail)
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (request == null) return NotFound();
        if (!CanAccessRequest(request, query ?? phone)) return Forbid();
        return View(request);
    }

    private async Task<OrderDetail?> LoadOrderDetailAsync(int id) => await _db.OrderDetails
        .Include(x => x.Order).ThenInclude(x => x!.User)
        .Include(x => x.Product)
        .FirstOrDefaultAsync(x => x.Id == id);

    private async Task<IActionResult?> ValidateCanCreateAsync(OrderDetail detail)
    {
        var redirectQuery = WarrantyCodeHelper.BuildWarrantyCode(detail.OrderId, detail.Id);
        if (!WarrantyPolicy.IsOrderEligible(detail.Order!))
        {
            TempData["ErrorMessage"] = "Sản phẩm chỉ có thể yêu cầu bảo hành sau khi đơn hàng đã thanh toán, giao hàng hoặc hoàn tất.";
            return RedirectToAction(nameof(Check), new { query = redirectQuery });
        }
        if (!WarrantyPolicy.IsInWarranty(detail.Order!.CreatedAt, GetWarrantyMonths(detail), DateTimeHelper.UtcNow()))
        {
            TempData["InfoMessage"] = "Sản phẩm đã hết thời hạn bảo hành, vui lòng liên hệ shop để được hỗ trợ thêm.";
            return RedirectToAction(nameof(Check), new { query = redirectQuery });
        }
        var duplicate = await _db.WarrantyRequests.AnyAsync(x => x.OrderDetailId == detail.Id &&
                                                                  WarrantyStatuses.AllActive.Contains(x.Status));
        if (duplicate)
        {
            TempData["InfoMessage"] = "Sản phẩm này đang có yêu cầu bảo hành được xử lý. Vui lòng theo dõi trạng thái bên dưới.";
            return RedirectToAction(nameof(Check), new { query = redirectQuery });
        }
        return null;
    }

    private WarrantyCreateVm BuildCreateVm(OrderDetail detail)
    {
        var vm = new WarrantyCreateVm
        {
            CustomerName = detail.Order!.User?.FullName ?? detail.Order.ReceiverName ?? string.Empty,
            Phone = detail.Order.User?.Phone ?? detail.Order.ReceiverPhone ?? string.Empty,
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
        vm.WarrantyCode = WarrantyCodeHelper.BuildWarrantyCode(detail.OrderId, detail.Id);
        vm.PurchaseDate = detail.Order!.CreatedAt;
        vm.WarrantyMonths = months;
        vm.ExpiresAt = WarrantyPolicy.ExpiresAt(detail.Order.CreatedAt, months);
    }

    private bool CanAccessOrder(Order order, string? phone = null)
    {
        if (User.IsInRole("Admin")) return true;
        var userId = CurrentUserId();
        if (order.UserId.HasValue && userId.HasValue && order.UserId == userId) return true;
        return HttpContext.Session.GetInt32("LastOrderId") == order.Id ||
               (!string.IsNullOrWhiteSpace(phone) && order.ReceiverPhone == phone.Trim());
    }

    private bool CanAccessRequest(WarrantyRequest request, string? lookup)
    {
        if (User.IsInRole("Admin")) return true;
        var userId = CurrentUserId();
        if (request.UserId.HasValue && userId.HasValue) return request.UserId == userId;
        if (string.IsNullOrWhiteSpace(lookup)) return false;

        var normalized = lookup.Trim();
        var orderCode = request.OrderId.HasValue ? $"DH{request.OrderId.Value:D6}" : string.Empty;
        return request.Phone == normalized || request.WarrantyCode == normalized || request.RequestCode == normalized ||
               orderCode.Equals(normalized, StringComparison.OrdinalIgnoreCase);
    }

    private void DeleteEvidenceFile(string? evidencePath)
    {
        if (string.IsNullOrWhiteSpace(evidencePath)) return;
        var relativePath = evidencePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_environment.WebRootPath, relativePath);
        if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
    }

    private int? CurrentUserId() => User.Identity?.IsAuthenticated == true &&
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static WarrantyProductVm BuildWarrantyProductVm(OrderDetail detail, bool hasActiveRequest, DateTime now)
    {
        var purchaseDate = detail.Order!.CreatedAt;
        var months = GetWarrantyMonthsOrNull(detail);
        var state = GetWarrantyState(purchaseDate, months, now);

        return new WarrantyProductVm
        {
            OrderId = detail.OrderId,
            OrderDetailId = detail.Id,
            CustomerName = detail.Order.User?.FullName ?? detail.Order.ReceiverName,
            OrderStatus = detail.Order.Status.ToString(),
            ProductName = detail.ProductName ?? string.Empty,
            ProductImage = detail.ProductImage,
            WarrantyCode = WarrantyCodeHelper.BuildWarrantyCode(detail.OrderId, detail.Id),
            LookupPhone = detail.Order.ReceiverPhone,
            PurchaseDate = purchaseDate,
            WarrantyMonths = months,
            ExpiresAt = months.HasValue ? WarrantyPolicy.ExpiresAt(purchaseDate, months.Value) : null,
            IsEligibleOrder = WarrantyPolicy.IsOrderEligible(detail.Order),
            WarrantyState = state,
            WarrantyProgressPercent = CalculateProgressPercent(purchaseDate, months, now),
            HasActiveRequest = hasActiveRequest,
            Components = BuildComponentWarrantyRows(detail, purchaseDate, now)
        };
    }

    private static List<WarrantyComponentVm> BuildComponentWarrantyRows(OrderDetail detail, DateTime purchaseDate, DateTime now)
    {
        var componentSpecs = ProductSpecDisplayHelper.TryParseComponentSpecs(detail.Product?.TechnicalSpecifications);
        if (componentSpecs.Count == 0) return [];

        return componentSpecs.Select((component, index) =>
        {
            var months = ParseWarrantyMonths(component.Warranty) ??
                         (detail.Product?.WarrantyMonths > 0 ? detail.Product.WarrantyMonths : (int?)null);
            return new WarrantyComponentVm
            {
                Stt = component.Stt.GetValueOrDefault(index + 1),
                Name = component.Description,
                Quantity = component.Quantity.GetValueOrDefault(1),
                RawWarranty = component.Warranty,
                WarrantyMonths = months,
                PurchaseDate = purchaseDate,
                ExpiresAt = months.HasValue ? WarrantyPolicy.ExpiresAt(purchaseDate, months.Value) : null,
                State = GetWarrantyState(purchaseDate, months, now),
                ProgressPercent = CalculateProgressPercent(purchaseDate, months, now)
            };
        }).Where(x => !string.IsNullOrWhiteSpace(x.Name)).ToList();
    }

    private static WarrantyState GetWarrantyState(DateTime purchaseDate, int? warrantyMonths, DateTime now)
    {
        if (!warrantyMonths.HasValue || warrantyMonths.Value <= 0) return WarrantyState.Contact;
        var expiresAt = WarrantyPolicy.ExpiresAt(purchaseDate, warrantyMonths.Value);
        if (now > expiresAt) return WarrantyState.Expired;
        return expiresAt <= now.AddDays(30) ? WarrantyState.ExpiringSoon : WarrantyState.Active;
    }

    private static int CalculateProgressPercent(DateTime purchaseDate, int? warrantyMonths, DateTime now)
    {
        if (!warrantyMonths.HasValue || warrantyMonths.Value <= 0) return 0;
        var expiresAt = WarrantyPolicy.ExpiresAt(purchaseDate, warrantyMonths.Value);
        var totalDays = Math.Max(1, (expiresAt - purchaseDate).TotalDays);
        var usedDays = Math.Clamp((now - purchaseDate).TotalDays, 0, totalDays);
        return (int)Math.Round(usedDays / totalDays * 100, MidpointRounding.AwayFromZero);
    }

    private static int GetWarrantyMonths(OrderDetail detail) => GetWarrantyMonthsOrNull(detail) ?? 12;

    private static int? GetWarrantyMonthsOrNull(OrderDetail detail) =>
        ParseWarrantyMonths(detail.Warranty) ??
        (detail.WarrantyMonths > 0 ? detail.WarrantyMonths : (int?)null) ??
        (detail.Product?.WarrantyMonths > 0 ? detail.Product.WarrantyMonths : (int?)null);

    private static int? ParseWarrantyMonths(string? warranty)
    {
        if (string.IsNullOrWhiteSpace(warranty)) return null;
        var match = Regex.Match(warranty, @"(?<months>\d+)\s*(th|tháng|thang|month|months)?", RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups["months"].Value, out var months) && months > 0 ? months : (int?)null;
    }
}
