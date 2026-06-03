using System.Text.Json;
using System.Text.RegularExpressions;
using Datn.PcStore.Data;
using Datn.PcStore.Helpers;
using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Route("[controller]")]
public class CompareController : Controller
{
    private const string SessionKey = "CompareProductIds";
    private static readonly string[] PreferredSpecOrder =
    {
        "CPU", "GPU", "RAM", "SSD", "Mainboard", "PSU", "Màn hình", "Case", "Bảo hành"
    };

    private static readonly Dictionary<string, string[]> SpecAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CPU"] = new[] { "cpu", "processor", "bộ vi xử lý", "vi xử lý", "core", "ryzen" },
        ["GPU"] = new[] { "gpu", "vga", "card màn hình", "card man hinh", "graphics", "rtx", "gtx", "radeon" },
        ["RAM"] = new[] { "ram", "memory", "bộ nhớ", "bo nho", "ddr" },
        ["SSD"] = new[] { "ssd", "hdd", "ổ cứng", "o cung", "storage", "nvme" },
        ["Mainboard"] = new[] { "mainboard", "main", "motherboard", "bo mạch chủ", "bo mach chu", "b760", "z790", "h610", "b650", "x670" },
        ["PSU"] = new[] { "psu", "nguồn", "nguon", "power", "watt", "650w", "750w" },
        ["Màn hình"] = new[] { "màn hình", "man hinh", "monitor", "display" },
        ["Case"] = new[] { "case", "vỏ", "vo may", "vỏ máy" },
        ["Bảo hành"] = new[] { "bảo hành", "bao hanh", "warranty" }
    };

    private readonly ApplicationDbContext _db;

    public CompareController(ApplicationDbContext db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var products = await GetSelectedProductsAsync();
        var compareProducts = products.Select(BuildCompareProduct).ToList();

        var labels = compareProducts
            .SelectMany(p => p.Specifications.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(GetSpecSortOrder)
            .ThenBy(x => x)
            .ToList();

        var model = new CompareIndexVm
        {
            Products = compareProducts,
            Rows = labels.Select(label => new CompareRowVm
            {
                Label = label,
                ProductAValue = compareProducts.ElementAtOrDefault(0)?.Specifications.GetValueOrDefault(label) ?? "-",
                ProductBValue = compareProducts.ElementAtOrDefault(1)?.Specifications.GetValueOrDefault(label) ?? "-"
            }).ToList()
        };

        return View(model);
    }

    [HttpGet("Add/{productId:int}")]
    public async Task<IActionResult> Add(int productId, string? returnUrl = null)
    {
        var exists = await _db.Products.AnyAsync(p => p.Id == productId && p.IsActive);
        if (!exists)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sản phẩm cần so sánh.";
            return RedirectToSafeReturnUrl(returnUrl);
        }

        var ids = GetCompareIds();
        if (!ids.Contains(productId))
        {
            if (ids.Count >= 2)
            {
                TempData["ErrorMessage"] = "Bạn chỉ có thể so sánh tối đa 2 sản phẩm.";
                return RedirectToSafeReturnUrl(returnUrl);
            }

            ids.Add(productId);
            SaveCompareIds(ids);
            TempData["SuccessMessage"] = "Đã thêm sản phẩm vào danh sách so sánh.";
        }
        else
        {
            TempData["SuccessMessage"] = "Sản phẩm đã có trong danh sách so sánh.";
        }

        return RedirectToSafeReturnUrl(returnUrl);
    }

    [HttpPost("Remove")]
    public IActionResult Remove(int productId, string? returnUrl = null)
    {
        var ids = GetCompareIds();
        if (ids.Remove(productId))
        {
            SaveCompareIds(ids);
            TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi danh sách so sánh.";
        }

        return RedirectToSafeReturnUrl(returnUrl);
    }

    [HttpGet("Clear")]
    public IActionResult Clear(string? returnUrl = null)
    {
        HttpContext.Session.Remove(SessionKey);
        TempData["SuccessMessage"] = "Đã xóa danh sách so sánh.";
        return RedirectToSafeReturnUrl(returnUrl);
    }

    private async Task<List<Product>> GetSelectedProductsAsync()
    {
        var ids = GetCompareIds();
        if (ids.Count == 0) return new List<Product>();

        var products = await _db.Products
            .Include(p => p.ProductImages.OrderBy(i => i.SortOrder))
            .Where(p => ids.Contains(p.Id) && p.IsActive)
            .ToListAsync();

        return ids.Select(id => products.FirstOrDefault(p => p.Id == id)).Where(p => p != null).Cast<Product>().ToList();
    }

    private CompareProductVm BuildCompareProduct(Product product)
    {
        var imageUrl = ImageUrlHelper.ResolveImageUrl(
            product.ProductImages.FirstOrDefault(x => x.IsPrimary)?.ImageUrl
            ?? product.ProductImages.OrderBy(x => x.SortOrder).FirstOrDefault()?.ImageUrl
            ?? product.ThumbnailImage,
            "/images/no-image.png");

        return CompareProductVm.FromProduct(product, imageUrl, ParseSpecifications(product));
    }

    private static Dictionary<string, string> ParseSpecifications(Product product)
    {
        var specs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var text = string.Join('\n', new[] { product.Specifications, product.DetailDescription, product.Description }
            .Where(x => !string.IsNullOrWhiteSpace(x)));

        var parts = Regex.Split(text, @"\r?\n|\||;|<br\s*/?>", RegexOptions.IgnoreCase)
            .Select(CleanSpecText)
            .Where(x => !string.IsNullOrWhiteSpace(x) && x != "1")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var genericIndex = 1;
        foreach (var part in parts)
        {
            var (label, value) = ExtractLabelValue(part);
            if (string.IsNullOrWhiteSpace(label))
            {
                label = $"Thông số {genericIndex++}";
                value = part;
            }

            if (!specs.ContainsKey(label))
            {
                specs[label] = string.IsNullOrWhiteSpace(value) ? part : value;
            }
        }

        if (!string.IsNullOrWhiteSpace(product.WarrantyDuration) || product.WarrantyMonths > 0)
        {
            specs.TryAdd("Bảo hành", !string.IsNullOrWhiteSpace(product.WarrantyDuration) ? product.WarrantyDuration : $"{product.WarrantyMonths} tháng");
        }

        return specs;
    }

    private static (string Label, string Value) ExtractLabelValue(string part)
    {
        var explicitMatch = Regex.Match(part, @"^(?<label>[^:：\-–—]{2,40})\s*[:：\-–—]\s*(?<value>.+)$");
        if (explicitMatch.Success)
        {
            var label = NormalizeSpecLabel(explicitMatch.Groups["label"].Value);
            var value = explicitMatch.Groups["value"].Value.Trim();
            return (label, value);
        }

        foreach (var label in PreferredSpecOrder)
        {
            if (!SpecAliases.TryGetValue(label, out var aliases)) continue;
            var alias = aliases.FirstOrDefault(a => ContainsToken(part, a));
            if (alias == null) continue;

            var value = Regex.Replace(part, $@"^\s*{Regex.Escape(alias)}\s*[:：\-–—]?\s*", string.Empty, RegexOptions.IgnoreCase).Trim();
            return (label, string.IsNullOrWhiteSpace(value) ? part : value);
        }

        return (string.Empty, string.Empty);
    }

    private static string NormalizeSpecLabel(string rawLabel)
    {
        var label = CleanSpecText(rawLabel).Trim(':', '-', '–', '—');
        foreach (var preferred in PreferredSpecOrder)
        {
            if (SpecAliases.TryGetValue(preferred, out var aliases) && aliases.Any(a => ContainsToken(label, a)))
            {
                return preferred;
            }
        }

        return label;
    }

    private static bool ContainsToken(string text, string token)
        => text.Contains(token, StringComparison.OrdinalIgnoreCase);

    private static string CleanSpecText(string value)
        => Regex.Replace(value ?? string.Empty, "<.*?>", string.Empty).Trim(' ', '-', '•', '\t', '\r', '\n');

    private static int GetSpecSortOrder(string label)
    {
        var index = Array.FindIndex(PreferredSpecOrder, x => x.Equals(label, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index : PreferredSpecOrder.Length;
    }

    private List<int> GetCompareIds()
    {
        var raw = HttpContext.Session.GetString(SessionKey);
        if (string.IsNullOrWhiteSpace(raw)) return new List<int>();

        try
        {
            return JsonSerializer.Deserialize<List<int>>(raw)?.Distinct().Take(2).ToList() ?? new List<int>();
        }
        catch (JsonException)
        {
            return new List<int>();
        }
    }

    private void SaveCompareIds(List<int> ids)
        => HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(ids.Distinct().Take(2).ToList()));

    private IActionResult RedirectToSafeReturnUrl(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        var referer = Request.Headers.Referer.ToString();
        if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var uri))
        {
            return Redirect(uri.PathAndQuery);
        }

        return RedirectToAction("Index", "Products");
    }
}
