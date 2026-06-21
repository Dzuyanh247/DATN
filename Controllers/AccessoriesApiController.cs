using Datn.PcStore.Data;
using Datn.PcStore.Helpers;
using Datn.PcStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[ApiController]
[Route("api/accessories")]
public class AccessoriesApiController : ControllerBase
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    { ComponentTypes.Monitor, ComponentTypes.Keyboard, ComponentTypes.Mouse, ComponentTypes.Headphone, "Headset" };

    private static readonly IReadOnlyDictionary<string, string> DefaultImages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [ComponentTypes.Monitor] = "https://ttgshop.vn/media/category/cat_big_1003486567.png",
        [ComponentTypes.Keyboard] = "https://ttgshop.vn/media/category/cat_big_1003353341.png",
        [ComponentTypes.Mouse] = "https://ttgshop.vn/media/category/cat_big_1003353370.png",
        [ComponentTypes.Headphone] = "https://ttgshop.vn/media/category/cat_big_1004013226.jpg",
        ["Headset"] = "https://ttgshop.vn/media/category/cat_big_1004013226.jpg"
    };

    private readonly ApplicationDbContext _db;
    public AccessoriesApiController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get(string type, string? keyword, decimal? minPrice, decimal? maxPrice, string? brand, int? categoryId, string? sort)
    {
        if (string.IsNullOrWhiteSpace(type) || !AllowedTypes.Contains(type))
            return BadRequest(new { message = "Loại sản phẩm mua kèm không hợp lệ." });

        var normalizedType = string.Equals(type, "Headset", StringComparison.OrdinalIgnoreCase) ? ComponentTypes.Headphone : ComponentTypes.Normalize(type);
        var typeAliases = ComponentTypes.GetAliases(normalizedType);
        var baseQuery = _db.Products.Include(p => p.Category).Include(p => p.ProductImages)
            .Where(p => p.IsActive && p.IsInStock && p.StockQuantity > 0 && p.ProductType == ProductKinds.Component && typeAliases.Contains(p.ComponentType ?? string.Empty));
        var query = baseQuery;

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(p => (p.Name ?? string.Empty).Contains(kw) || (p.Brand != null && p.Brand.Contains(kw)) || (p.ShortDescription != null && p.ShortDescription.Contains(kw)));
        }
        if (minPrice.HasValue) query = query.Where(p => ((p.DiscountPrice ?? p.SalePrice) ?? p.Price) >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(p => ((p.DiscountPrice ?? p.SalePrice) ?? p.Price) <= maxPrice.Value);
        if (!string.IsNullOrWhiteSpace(brand)) query = query.Where(p => p.Brand == brand);
        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);

        query = (sort ?? "newest").ToLowerInvariant() switch
        {
            "price_asc" => query.OrderBy(p => (p.DiscountPrice ?? p.SalePrice) ?? p.Price),
            "price_desc" => query.OrderByDescending(p => (p.DiscountPrice ?? p.SalePrice) ?? p.Price),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var productRows = await query.Take(60).ToListAsync();
        var products = productRows.Select(p =>
        {
            var price = (p.DiscountPrice ?? p.SalePrice) ?? p.Price;
            return new
            {
                id = p.Id,
                name = p.Name ?? "Sản phẩm không xác định",
                brand = p.Brand ?? string.Empty,
                categoryId = p.CategoryId,
                categoryName = p.Category?.Name ?? string.Empty,
                image = ImageUrlHelper.ResolveImageUrl(p.ProductImages.OrderBy(x => x.SortOrder).Select(x => x.ImageUrl).FirstOrDefault() ?? p.ThumbnailImage, DefaultImages[normalizedType]),
                oldPrice = p.Price,
                price,
                discountPercent = p.Price > price ? Math.Round((p.Price - price) * 100 / p.Price) : 0,
                stockQuantity = p.StockQuantity,
                detailUrl = Url.Action("Detail", "Products", new { id = p.Id })
            };
        }).ToList();

        var facetRows = await baseQuery.Select(p => new { p.Brand, p.CategoryId, CategoryName = p.Category != null ? (p.Category.Name ?? string.Empty) : string.Empty }).ToListAsync();
        var facets = new
        {
            brands = facetRows.Where(p => !string.IsNullOrWhiteSpace(p.Brand) && p.Brand != "N/A").Select(p => p.Brand).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList(),
            categories = facetRows.GroupBy(p => p.CategoryId).Select(g => new { categoryId = g.Key, name = g.Select(x => x.CategoryName).FirstOrDefault() ?? string.Empty }).OrderBy(x => x.name).ToList()
        };

        return Ok(new { products, facets });
    }
}
