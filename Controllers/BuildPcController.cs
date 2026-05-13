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

    public BuildPcController(ApplicationDbContext db, ICartService cartService, BuildCompatibilityService compatibilityService)
    {
        _db = db;
        _cartService = cartService;
        _compatibilityService = compatibilityService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var vm = await BuildViewModelAsync();
        return View(vm);
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProductsByComponent(string type, string? keyword, string? sort)
    {
        var products = await QueryProductsByType(type);
        if (!string.IsNullOrWhiteSpace(keyword)) products = products.Where(x => x.Name.Contains(keyword));
        products = sort == "price_desc" ? products.OrderByDescending(x => x.Price) : products.OrderBy(x => x.Price);
        var result = products.Select(p => new BuildProductOptionViewModel
        {
            Id = p.Id,
            Name = p.Name,
            ImageUrl = p.ThumbnailImage,
            Price = p.DiscountPrice ?? p.SalePrice ?? p.Price,
            StockQuantity = p.StockQuantity
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

    private async Task<BuildPcViewModel> BuildViewModelAsync()
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
        type = type.ToUpperInvariant();
        var query = _db.Products.Include(x => x.Category).Where(x => x.IsActive && x.IsInStock);
        return await query.Where(p => MatchType(type, p)).ToListAsync();
    }

    private static bool MatchType(string type, Product p)
    {
        var cat = p.Category?.Name ?? string.Empty;
        var comp = p.ComponentType ?? string.Empty;
        var source = $"{cat} {comp}".ToLowerInvariant();
        return type switch
        {
            "CPU" => source.Contains("cpu") || source.Contains("vi xử lý"),
            "MAINBOARD" => source.Contains("mainboard") || source.Contains("bo mạch chủ"),
            "RAM" => source.Contains("ram"),
            "GPU" => source.Contains("vga") || source.Contains("card đồ họa") || source.Contains("gpu"),
            "STORAGE" => source.Contains("ssd") || source.Contains("hdd") || source.Contains("ổ cứng"),
            "PSU" => source.Contains("nguồn") || source.Contains("psu"),
            "COOLER" => source.Contains("tản nhiệt") || source.Contains("cooler"),
            "CASE" => source.Contains("case") || source.Contains("vỏ case"),
            "MONITOR" => source.Contains("màn hình") || source.Contains("monitor"),
            _ => false
        };
    }

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
