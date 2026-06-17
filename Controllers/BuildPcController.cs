using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Datn.PcStore.Services;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Route("buildpc")]
public class BuildPcController : Controller
{
    private const string BuildSessionKey = "buildpc_selected";
    private readonly ApplicationDbContext _db;
    private readonly ICartService _cartService;
    private readonly BuildCompatibilityService _compatibilityService;
    private readonly ILogger<BuildPcController> _logger;

    private static readonly (string Type, string Display)[] ComponentOrder =
    [
        ("CPU", "CPU"),
        ("MAINBOARD", "MAINBOARD"),
        ("RAM", "RAM"),
        ("GPU", "CARD ĐỒ HỌA"),
        ("STORAGE", "Ổ CỨNG"),
        ("PSU", "NGUỒN (PSU)"),
        ("COOLER", "TẢN NHIỆT"),
        ("CASE", "VỎ CASE"),
        ("MONITOR", "MÀN HÌNH")
    ];

    public BuildPcController(ApplicationDbContext db, ICartService cartService, BuildCompatibilityService compatibilityService, ILogger<BuildPcController> logger)
    {
        _db = db;
        _cartService = cartService;
        _compatibilityService = compatibilityService;
        _logger = logger;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        var vm = BuildViewModel();
        return View(vm);
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProductsByComponent(string type, string? keyword, string? sort)
    {
        var displayType = NormalizeType(type);
        var normalizedType = NormalizeBuildTypeToComponentType(displayType);
        var products = await QueryProductsByType(normalizedType);
        if (!string.IsNullOrWhiteSpace(keyword)) products = products.Where(x => x.Name.Contains(keyword)).ToList();
        products = sort == "price_desc" ? products.OrderByDescending(x => x.Price).ToList() : products.OrderBy(x => x.Price).ToList();

        _logger.LogInformation("Build PC products for type {Type}: {Count} item(s)", normalizedType, products.Count);

        var result = products.Select(p => new BuildProductOptionViewModel
        {
            Id = p.Id,
            Name = p.Name,
            ImageUrl = p.ThumbnailImage,
            Price = p.DiscountPrice ?? p.SalePrice ?? p.Price,
            CategoryName = p.Category?.Name ?? string.Empty,
            StockQuantity = p.StockQuantity,
            Warranty = p.WarrantyDuration ?? (p.WarrantyMonths > 0 ? $"{p.WarrantyMonths} tháng" : string.Empty)
        }).ToList();
        return Json(result);
    }

    [HttpPost("select")]
    public async Task<IActionResult> SelectComponent(string type, int productId)
    {
        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == productId && x.IsActive);
        if (product == null) return NotFound();

        var selected = GetSelectedFromSession();
        selected[type.ToUpperInvariant()] = new SelectedComponentViewModel
        {
            ProductId = product.Id,
            Type = type.ToUpperInvariant(),
            ProductName = product.Name,
            ImageUrl = product.ThumbnailImage,
            Price = product.DiscountPrice ?? product.SalePrice ?? product.Price,
            Quantity = 1
        };
        _compatibilityService.IsCompatible(selected.Values, product, out var warning);
        SaveSelectedToSession(selected);
        return Json(new { success = true, warning });
    }

    [HttpPost("remove")]
    public IActionResult RemoveComponent(string type)
    {
        var selected = GetSelectedFromSession();
        selected.Remove(type.ToUpperInvariant());
        SaveSelectedToSession(selected);
        return Json(new { success = true });
    }

    [HttpPost("reset")]
    public IActionResult ResetBuild()
    {
        HttpContext.Session.Remove(BuildSessionKey);
        return Json(new { success = true });
    }

    [HttpPost("add-to-cart")]
    public async Task<IActionResult> AddBuildToCart()
    {
        var selected = GetSelectedFromSession();
        int? userId = User.Identity?.IsAuthenticated == true ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) : null;
        foreach (var item in selected.Values)
            await _cartService.AddToCartAsync(userId, item.ProductId, item.Quantity);

        TempData["SuccessMessage"] = "Đã thêm toàn bộ cấu hình vào giỏ hàng.";
        return RedirectToAction("Index", "Cart");
    }

    [HttpGet("export-csv")]
    public IActionResult ExportCsv()
    {
        var selected = GetSelectedFromSession();
        var sb = new StringBuilder();
        sb.AppendLine("Loai linh kien,San pham,Don gia,So luong,Thanh tien");
        foreach (var item in selected.Values)
            sb.AppendLine($"{item.Type},{item.ProductName},{item.Price},{item.Quantity},{item.Price * item.Quantity}");
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "buildpc-config.csv");
    }

    private BuildPcViewModel BuildViewModel()
    {
        var selected = GetSelectedFromSession();
        var vm = new BuildPcViewModel();
        foreach (var item in ComponentOrder)
        {
            selected.TryGetValue(item.Type, out var selectedItem);
            vm.Components.Add(new BuildPcComponentViewModel { Type = item.Type, DisplayName = item.Display, Selected = selectedItem });
        }
        return vm;
    }

    private async Task<List<Product>> QueryProductsByType(string type)
    {
        var products = await _db.Products
            .Include(x => x.Category)
            .Where(x => x.IsActive && x.IsInStock && (x.ProductType == ProductKinds.Component || x.ProductType.Contains("Linh")))
            .ToListAsync();

        return products
            .Where(p => string.Equals(ComponentTypes.Normalize(p.ComponentType), type, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static string NormalizeType(string? type)
    {
        var value = (type ?? string.Empty).Trim().ToUpperInvariant();
        return value switch
        {
            "CARD ĐỒ HỌA" => "GPU",
            "Ổ CỨNG" => "STORAGE",
            "NGUỒN" => "PSU",
            "TẢN NHIỆT" => "COOLER",
            "VỎ CASE" => "CASE",
            "MÀN HÌNH" => "MONITOR",
            _ => value
        };
    }

    private static string NormalizeBuildTypeToComponentType(string type) => type switch
    {
        "MAINBOARD" => ComponentTypes.Mainboard,
        "GPU" => ComponentTypes.VGA,
        "STORAGE" => ComponentTypes.Storage,
        "PSU" => ComponentTypes.PSU,
        "COOLER" => ComponentTypes.Cooler,
        "CASE" => ComponentTypes.Case,
        "MONITOR" => ComponentTypes.Monitor,
        _ => ComponentTypes.Normalize(type)
    };

    private Dictionary<string, SelectedComponentViewModel> GetSelectedFromSession()
    {
        var raw = HttpContext.Session.GetString(BuildSessionKey);
        return string.IsNullOrWhiteSpace(raw)
            ? new Dictionary<string, SelectedComponentViewModel>()
            : JsonSerializer.Deserialize<Dictionary<string, SelectedComponentViewModel>>(raw) ?? new Dictionary<string, SelectedComponentViewModel>();
    }

    private void SaveSelectedToSession(Dictionary<string, SelectedComponentViewModel> selected)
        => HttpContext.Session.SetString(BuildSessionKey, JsonSerializer.Serialize(selected));
}
