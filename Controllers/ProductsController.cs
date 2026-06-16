using Datn.PcStore.Data;
using Datn.PcStore.Helpers;
using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Datn.PcStore.Services;

namespace Datn.PcStore.Controllers;

public class ProductsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IProductReviewService _reviewService;
    public ProductsController(ApplicationDbContext db, IProductReviewService reviewService)
    {
        _db = db;
        _reviewService = reviewService;
    }

    public async Task<IActionResult> Index(
        string? keyword,
        int? categoryId,
        string? categorySlug,
        string? brand,
        decimal? minPrice,
        decimal? maxPrice,
        string[]? priceRanges,
        string[]? brands,
        string[]? cpu,
        string[]? ram,
        string[]? gpu)
    {
        var vm = new ProductFilterVm
        {
            Keyword = keyword,
            CategoryId = categoryId,
            CategorySlug = categorySlug,
            Brand = brand,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            PriceRanges = CleanSelections(priceRanges),
            Brands = CleanSelections(brands),
            Cpu = CleanSelections(cpu),
            Ram = CleanSelections(ram),
            Gpu = CleanSelections(gpu)
        };

        vm.Keyword = vm.Keyword?.Trim();
        var query = _db.Products.Include(p => p.Category).AsNoTracking().AsQueryable();
        if (!vm.CategoryId.HasValue && !string.IsNullOrWhiteSpace(vm.CategorySlug))
        {
            var normalizedSlug = vm.CategorySlug.Trim().ToLowerInvariant();
            vm.CategoryId = await _db.Categories
                .Where(c => c.Name.ToLower().Replace(" ", "-") == normalizedSlug)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync();
        }

        if (vm.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == vm.CategoryId.Value);
        if (!string.IsNullOrWhiteSpace(vm.Brand))
            query = query.Where(p => p.Brand == vm.Brand);
        if (vm.MinPrice.HasValue)
            query = query.Where(p => ((p.DiscountPrice ?? p.SalePrice) ?? p.Price) >= vm.MinPrice.Value);
        if (vm.MaxPrice.HasValue)
            query = query.Where(p => ((p.DiscountPrice ?? p.SalePrice) ?? p.Price) <= vm.MaxPrice.Value);

        var candidateProducts = await query
            .Include(product => product.ProductImages)
            .OrderByDescending(product => product.CreatedAt)
            .ToListAsync();
        var facetProducts = ApplyKeywordSearch(candidateProducts, vm);
        var parsedFacets = facetProducts.ToDictionary(product => product.Id, ProductFilterFacetHelper.Parse);
        PopulateFilterOptions(vm, facetProducts, parsedFacets);

        IEnumerable<Product> filteredProducts = facetProducts;
        if (vm.PriceRanges.Length > 0)
            filteredProducts = ApplyPriceRangeFilter(filteredProducts, vm.PriceRanges);
        if (vm.Brands.Length > 0)
            filteredProducts = filteredProducts.Where(product => !string.IsNullOrWhiteSpace(product.Brand) && vm.Brands.Contains(product.Brand, StringComparer.OrdinalIgnoreCase));

        var matchingIds = GetMatchingParsedFacetIds(facetProducts, parsedFacets, vm);
        if (matchingIds is not null)
            filteredProducts = filteredProducts.Where(product => matchingIds.Contains(product.Id));

        vm.Categories = await _db.Categories.OrderBy(category => category.Name).ToListAsync();
        vm.Products = filteredProducts.ToList();

        return View(vm);
    }

    public async Task<IActionResult> Detail(int id, int? rating)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.ProductImages.OrderBy(x => x.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        ViewBag.UpgradeSuggestions = await _db.Products
            .Include(p => p.ProductImages)
            .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.Price > product.Price)
            .OrderBy(p => p.Price)
            .Take(4)
            .ToListAsync();
        var userId = User.Identity?.IsAuthenticated == true
            ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
            : (int?)null;
        ViewBag.ReviewSection = await _reviewService.GetSectionAsync(id, userId, rating is >= 1 and <= 5 ? rating : null);

        var accessoryProducts = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.ProductImages.OrderBy(x => x.SortOrder))
            .Where(p => p.IsActive
                && p.IsInStock
                && p.StockQuantity > 0
                && p.ProductType == ProductKinds.Component
                && (p.ComponentType == "Monitor" || p.ComponentType == "Keyboard" || p.ComponentType == "Mouse" || p.ComponentType == "Headphone"))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var vm = new ProductDetailViewModel
        {
            Product = product,
            Monitors = accessoryProducts.Where(p => p.ComponentType == "Monitor").ToList(),
            Keyboards = accessoryProducts.Where(p => p.ComponentType == "Keyboard").ToList(),
            Mice = accessoryProducts.Where(p => p.ComponentType == "Mouse").ToList(),
            Headsets = accessoryProducts.Where(p => p.ComponentType == "Headphone").ToList()
        };

        return View(vm);
    }

    private static string[] CleanSelections(string[]? values) =>
        values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();

    private static void PopulateFilterOptions(
        ProductFilterVm vm,
        IReadOnlyCollection<Product> products,
        IReadOnlyDictionary<int, ProductParsedFacets> parsedFacets)
    {
        vm.PriceRangeOptions = ProductFilterFacetHelper.PriceRanges
            .Select(range => new ProductFilterOptionVm
            {
                Value = range.Value,
                Label = range.Label,
                Count = products.Count(product => ProductFilterFacetHelper.IsInPriceRange(
                    ProductFilterFacetHelper.GetEffectivePrice(product), range))
            })
            .ToList();

        vm.BrandOptions = products
            .Where(product => !string.IsNullOrWhiteSpace(product.Brand) && !string.Equals(product.Brand.Trim(), "N/A", StringComparison.OrdinalIgnoreCase))
            .GroupBy(product => product.Brand.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new ProductFilterOptionVm { Value = group.Key, Label = group.Key, Count = group.Count() })
            .OrderBy(option => option.Label)
            .ToList();

        vm.CpuOptions = BuildParsedOptions(products, parsedFacets, facets => facets.Cpu)
            .OrderBy(option => option.Label)
            .ToList();
        vm.RamOptions = BuildParsedOptions(products, parsedFacets, facets => facets.Ram)
            .OrderBy(option => ParseRamCapacity(option.Value))
            .ToList();
        vm.GpuOptions = BuildParsedOptions(products, parsedFacets, facets => facets.Gpu)
            .OrderBy(option => option.Label)
            .ToList();
    }

    private static IEnumerable<ProductFilterOptionVm> BuildParsedOptions(
        IEnumerable<Product> products,
        IReadOnlyDictionary<int, ProductParsedFacets> parsedFacets,
        Func<ProductParsedFacets, HashSet<string>> selector) =>
        products
            .SelectMany(product => selector(parsedFacets[product.Id]).Select(value => new { product.Id, Value = value }))
            .GroupBy(item => item.Value, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ProductFilterOptionVm
            {
                Value = group.Key,
                Label = group.Key,
                Count = group.Select(item => item.Id).Distinct().Count()
            });

    private static int ParseRamCapacity(string value) =>
        int.TryParse(value.Replace("GB", string.Empty, StringComparison.OrdinalIgnoreCase), out var capacity)
            ? capacity
            : int.MaxValue;

    private static HashSet<int>? GetMatchingParsedFacetIds(
        IEnumerable<Product> products,
        IReadOnlyDictionary<int, ProductParsedFacets> parsedFacets,
        ProductFilterVm vm)
    {
        if (vm.Cpu.Length == 0 && vm.Ram.Length == 0 && vm.Gpu.Length == 0)
            return null;

        return products
            .Where(product => MatchesAny(parsedFacets[product.Id].Cpu, vm.Cpu))
            .Where(product => MatchesAny(parsedFacets[product.Id].Ram, vm.Ram))
            .Where(product => MatchesAny(parsedFacets[product.Id].Gpu, vm.Gpu))
            .Select(product => product.Id)
            .ToHashSet();
    }

    private static bool MatchesAny(HashSet<string> productValues, string[] selectedValues) =>
        selectedValues.Length == 0 || selectedValues.Any(productValues.Contains);

    private static List<Product> ApplyKeywordSearch(List<Product> products, ProductFilterVm vm)
    {
        if (!vm.HasKeyword)
            return products;

        var normalizedKeyword = SearchTextHelper.NormalizeSearchText(vm.Keyword);
        var searchableProducts = products.Select(product => new
        {
            Product = product,
            SearchText = SearchTextHelper.NormalizeSearchText(string.Join(' ',
                product.Name, product.Brand ?? string.Empty, product.Category?.Name, product.ShortDescription,
                product.Description, product.DetailDescription, product.Specifications,
                product.ComponentType, product.CpuSocket, product.RamType))
        }).ToList();

        var exactMatches = searchableProducts
            .Where(item => item.SearchText.Contains(normalizedKeyword, StringComparison.Ordinal))
            .Select(item => item.Product)
            .ToList();
        if (exactMatches.Count > 0)
            return exactMatches;

        var tokens = SearchTextHelper.Tokenize(vm.Keyword);
        if (tokens.Length == 0)
            return [];

        var minimumMatches = tokens.Length == 1 ? 1 : Math.Max(2, (int)Math.Ceiling(tokens.Length * 0.6));
        var fallbackMatches = searchableProducts
            .Select(item => new
            {
                item.Product,
                Score = tokens.Count(token => item.SearchText.Contains(token, StringComparison.Ordinal))
            })
            .Where(item => item.Score >= minimumMatches)
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Product.CreatedAt)
            .Select(item => item.Product)
            .ToList();

        vm.IsEquivalentSearch = fallbackMatches.Count > 0;
        return fallbackMatches;
    }

    private static IEnumerable<Product> ApplyPriceRangeFilter(IEnumerable<Product> products, string[] selectedValues)
    {
        var selectedRanges = ProductFilterFacetHelper.PriceRanges
            .Where(range => selectedValues.Contains(range.Value, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (selectedRanges.Count == 0)
            return products;

        return products.Where(product => selectedRanges.Any(range =>
            ProductFilterFacetHelper.IsInPriceRange(ProductFilterFacetHelper.GetEffectivePrice(product), range)));
    }
}
