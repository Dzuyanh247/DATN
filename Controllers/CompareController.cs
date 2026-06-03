using Datn.PcStore.Data;
using Datn.PcStore.Helpers;
using Datn.PcStore.Models;
using Datn.PcStore.Services;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Route("[controller]")]
public class CompareController : Controller
{
    private static readonly string[] PreferredSpecOrder =
    {
        "CPU", "RAM", "GPU", "SSD", "Mainboard", "PSU/Nguồn", "Case", "Tản nhiệt"
    };

    private static readonly Dictionary<string, string[]> ComponentKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CPU"] = new[] { "CPU", "Intel Core", "Core i", "Ryzen" },
        ["RAM"] = new[] { "RAM", "Ram", "DDR4", "DDR5", "Bus" },
        ["GPU"] = new[] { "Card màn hình", "Card Màn Hình", "VGA", "RTX", "GTX", "Radeon", "RX", "GeForce" },
        ["SSD"] = new[] { "SSD", "Ổ cứng", "Ổ Cứng", "NVMe", "M.2" },
        ["Mainboard"] = new[] { "Mainboard", "Bo mạch chủ", "B760", "H610", "B650", "A620", "Z790", "PRIME", "BATTLE-AX" },
        ["PSU/Nguồn"] = new[] { "Nguồn", "PSU", "650W", "750W", "850W", "80 Plus", "Bronze", "Gold" },
        ["Case"] = new[] { "Case", "Vỏ Case", "Vỏ", "Fan ARGB" },
        ["Tản nhiệt"] = new[] { "Tản nhiệt", "Tản Nhiệt", "Cooler", "Thermalright", "JONSBO", "IDCOOLING" }
    };

    private static readonly string[] ClassificationPriority =
    {
        "GPU", "CPU", "Mainboard", "RAM", "SSD", "PSU/Nguồn", "Case", "Tản nhiệt"
    };

    private readonly ApplicationDbContext _db;
    private readonly ICompareService _compareService;

    public CompareController(ApplicationDbContext db, ICompareService compareService)
    {
        _db = db;
        _compareService = compareService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var products = await _compareService.GetProductsAsync();
        var compareProducts = products.Select(BuildCompareProduct).ToList();

        var model = new CompareIndexVm
        {
            Products = compareProducts,
            Rows = PreferredSpecOrder.Select(label => new CompareRowVm
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

        if (_compareService.Contains(productId))
        {
            TempData["InfoMessage"] = "Sản phẩm đã nằm trong danh sách so sánh.";
            return RedirectToSafeReturnUrl(returnUrl);
        }

        if (_compareService.GetIds().Count >= CompareSessionService.MaxCompareProducts)
        {
            TempData["ErrorMessage"] = "Bạn chỉ có thể so sánh tối đa 2 sản phẩm. Hãy xóa một sản phẩm trước.";
            return RedirectToSafeReturnUrl(returnUrl);
        }

        _compareService.Add(productId);
        TempData["SuccessMessage"] = "Đã thêm sản phẩm vào danh sách so sánh.";
        return RedirectToSafeReturnUrl(returnUrl);
    }

    [HttpPost("Remove")]
    public IActionResult Remove(int productId, string? returnUrl = null)
    {
        if (_compareService.Remove(productId))
        {
            TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi danh sách so sánh.";
        }

        return RedirectToSafeReturnUrl(returnUrl);
    }

    [HttpGet("Clear")]
    public IActionResult Clear(string? returnUrl = null)
    {
        _compareService.Clear();
        TempData["SuccessMessage"] = "Đã xóa danh sách so sánh.";
        return RedirectToSafeReturnUrl(returnUrl);
    }

    private static CompareProductVm BuildCompareProduct(Product product)
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
        var componentSpecs = ProductComponentSpecHelper.TryDeserialize(product.TechnicalSpecifications);

        if (componentSpecs.Any())
        {
            foreach (var component in componentSpecs)
            {
                AddMatchedComponent(specs, component.Description);
            }

            return specs;
        }

        foreach (var component in ProductComponentSpecHelper.ParseFallbackText(product.TechnicalSpecifications))
        {
            AddMatchedComponent(specs, component.Description);
        }

        return specs;
    }

    private static void AddMatchedComponent(Dictionary<string, string> specs, string? rawDescription)
    {
        var description = NormalizeDisplayDescription(rawDescription);
        if (string.IsNullOrWhiteSpace(description)) return;

        var label = ClassifyComponent(description);
        if (string.IsNullOrWhiteSpace(label)) return;

        if (specs.TryGetValue(label, out var existing) && !string.IsNullOrWhiteSpace(existing))
        {
            specs[label] = $"{existing}\n{description}";
        }
        else
        {
            specs[label] = description;
        }
    }

    private static string ClassifyComponent(string description)
    {
        foreach (var label in ClassificationPriority)
        {
            if (ComponentKeywords.TryGetValue(label, out var keywords)
                && keywords.Any(keyword => description.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                return label;
            }
        }

        return string.Empty;
    }

    private static string NormalizeDisplayDescription(string? value)
        => (value ?? string.Empty).Trim();

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
