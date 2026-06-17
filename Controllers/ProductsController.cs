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
        string? type,
        decimal? minPrice,
        decimal? maxPrice,
        string[]? priceRanges,
        string[]? brands,
        string[]? componentTypes,
        string[]? specs,
        string[]? cpu,
        string[]? ram,
        string[]? gpu,
        string? sort)
    {
        var vm = new ProductFilterVm
        {
            Keyword = keyword,
            CategoryId = categoryId,
            CategorySlug = categorySlug,
            Brand = brand,
            Sort = NormalizeSort(sort),
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            PriceRanges = CleanSelections(priceRanges),
            Brands = CleanSelections(brands).Concat(CleanSelections(string.IsNullOrWhiteSpace(brand) ? null : new[] { brand })).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            ComponentTypes = CleanSelections(componentTypes).Concat(CleanSelections(string.IsNullOrWhiteSpace(type) ? null : new[] { type })).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            Specs = CleanSelections(specs),
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

        var baseProducts = await query
            .ToListAsync();
        var facetProducts = ApplyKeywordSearch(baseProducts, vm);
        var parsedFacets = facetProducts.ToDictionary(product => product.Id, ProductFilterFacetHelper.Parse);
        PopulateFilterOptions(vm, facetProducts, parsedFacets);

        if (vm.ComponentTypes.Length > 0)
            query = query.Where(p => p.ComponentType != null && vm.ComponentTypes.Contains(p.ComponentType));
        if (vm.MinPrice.HasValue)
            query = query.Where(p => ((p.DiscountPrice ?? p.SalePrice) ?? p.Price) >= vm.MinPrice.Value);
        if (vm.MaxPrice.HasValue)
            query = query.Where(p => ((p.DiscountPrice ?? p.SalePrice) ?? p.Price) <= vm.MaxPrice.Value);
        query = ApplyPriceRangeQuery(query, vm.PriceRanges);

        var candidateProducts = await query
            .Include(product => product.ProductImages)
            .OrderByDescending(product => product.CreatedAt)
            .ToListAsync();
        var filteredFacetProducts = ApplyKeywordSearch(candidateProducts, vm);
        var filteredParsedFacets = filteredFacetProducts.ToDictionary(product => product.Id, ProductFilterFacetHelper.Parse);

        IEnumerable<Product> filteredProducts = filteredFacetProducts;
        if (vm.Brands.Length > 0)
            filteredProducts = filteredProducts.Where(product => product.Brand != null && vm.Brands.Contains(product.Brand, StringComparer.OrdinalIgnoreCase));

        var matchingIds = GetMatchingParsedFacetIds(filteredFacetProducts, filteredParsedFacets, vm);
        if (matchingIds is not null)
            filteredProducts = filteredProducts.Where(product => matchingIds.Contains(product.Id));
        if (vm.Specs.Length > 0)
            filteredProducts = ApplySpecFilter(filteredProducts, vm.Specs);

        vm.Categories = await _db.Categories.OrderBy(category => category.Name).ToListAsync();
        vm.Products = ApplySort(filteredProducts, vm.Sort).ToList();

        return View(vm);
    }

    public Task<IActionResult> Category(string? type, string? brand)
    {
        return Index(null, null, null, brand, type, null, null, null, null, null, null, null, null, null, null);
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


    private static string NormalizeSort(string? sort) =>
        sort?.Trim().ToLowerInvariant() switch
        {
            "price_asc" => "price_asc",
            "price_desc" => "price_desc",
            "name_asc" => "name_asc",
            _ => "newest"
        };

    private static IEnumerable<Product> ApplySort(IEnumerable<Product> products, string? sort) =>
        NormalizeSort(sort) switch
        {
            "price_asc" => products.OrderBy(ProductFilterFacetHelper.GetEffectivePrice).ThenByDescending(product => product.CreatedAt),
            "price_desc" => products.OrderByDescending(ProductFilterFacetHelper.GetEffectivePrice).ThenByDescending(product => product.CreatedAt),
            "name_asc" => products.OrderBy(product => product.Name),
            _ => products.OrderByDescending(product => product.CreatedAt)
        };

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
            .GroupBy(product => product.Brand!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new ProductFilterOptionVm { Value = group.Key, Label = group.Key, Count = group.Count() })
            .OrderBy(option => option.Label)
            .ToList();

        vm.ComponentTypeGroups = BuildComponentTypeGroups(products);
        vm.SpecFilterGroups = BuildSpecFilterGroups(products);

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

    private static List<ProductFilterGroupVm> BuildComponentTypeGroups(IReadOnlyCollection<Product> products)
    {
        var counts = products
            .Where(product => !string.IsNullOrWhiteSpace(product.ComponentType))
            .GroupBy(product => product.ComponentType!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        return
        [
            new ProductFilterGroupVm
            {
                Title = "Linh kiện máy tính",
                Options = BuildComponentOptions(counts,
                    ("CPU", "CPU"),
                    ("Mainboard", "Mainboard"),
                    ("RAM", "RAM"),
                    ("VGA", "VGA - Card màn hình"),
                    ("SSD", "Ổ cứng SSD"),
                    ("HDD", "Ổ cứng HDD"),
                    ("Storage", "Ổ cứng (SSD, HDD)"),
                    ("Cooler", "Tản nhiệt"),
                    ("Case", "Vỏ case"),
                    ("PSU", "Nguồn (PSU)"))
            },
            new ProductFilterGroupVm
            {
                Title = "Ngoại vi",
                Options = BuildComponentOptions(counts,
                    ("Monitor", "Màn hình"),
                    ("Keyboard", "Bàn phím"),
                    ("Mouse", "Chuột"),
                    ("Headphone", "Tai nghe"))
            }
        ];
    }

    private static List<ProductFilterOptionVm> BuildComponentOptions(Dictionary<string, int> counts, params (string Value, string Label)[] options) =>
        options
            .Select(option => new ProductFilterOptionVm
            {
                Value = option.Value,
                Label = option.Label,
                Count = counts.GetValueOrDefault(option.Value)
            })
            .ToList();

    private static List<ProductSpecFilterGroupVm> BuildSpecFilterGroups(IEnumerable<Product> products) =>
        products
            .SelectMany(product => ProductSpecificationKeyValueHelper.ParseStored(product.Specifications)
                .Where(spec => !string.IsNullOrWhiteSpace(spec.Name) && !string.IsNullOrWhiteSpace(spec.Value))
                .Select(spec => new { product.Id, Name = spec.Name.Trim(), Value = spec.Value.Trim() }))
            .GroupBy(spec => spec.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Select(spec => spec.Value).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            .OrderBy(group => GetSpecGroupSortOrder(group.Key))
            .ThenBy(group => group.Key)
            .Take(8)
            .Select(group => new ProductSpecFilterGroupVm
            {
                Name = group.Key,
                Options = group
                    .GroupBy(spec => spec.Value, StringComparer.OrdinalIgnoreCase)
                    .Select(valueGroup => new ProductFilterOptionVm
                    {
                        Value = BuildSpecValue(group.Key, valueGroup.Key),
                        Label = valueGroup.Key,
                        Count = valueGroup.Select(spec => spec.Id).Distinct().Count()
                    })
                    .OrderBy(option => option.Label)
                    .Take(12)
                    .ToList()
            })
            .Where(group => group.Options.Count > 0)
            .ToList();

    private static int GetSpecGroupSortOrder(string name)
    {
        var normalized = name.ToLowerInvariant();
        if (normalized.Contains("socket")) return 0;
        if (normalized.Contains("nhân") || normalized.Contains("nhan")) return 1;
        if (normalized.Contains("luồng") || normalized.Contains("luong")) return 2;
        if (normalized.Contains("bus")) return 3;
        if (normalized.Contains("dung") || normalized.Contains("bộ nhớ") || normalized.Contains("bo nho") || normalized.Contains("vram")) return 4;
        if (normalized.Contains("kết nối") || normalized.Contains("ket noi")) return 5;
        return 10;
    }

    private static string BuildSpecValue(string name, string value) => $"{name.Trim()}::{value.Trim()}";

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

    private static IQueryable<Product> ApplyPriceRangeQuery(IQueryable<Product> query, string[] selectedValues)
    {
        var selectedRanges = ProductFilterFacetHelper.PriceRanges
            .Where(range => selectedValues.Contains(range.Value, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (selectedRanges.Count == 0)
            return query;

        return query.Where(product =>
            (selectedValues.Contains("under-1") && ((product.DiscountPrice ?? product.SalePrice) ?? product.Price) < 1_000_000m) ||
            (selectedValues.Contains("1-2") && ((product.DiscountPrice ?? product.SalePrice) ?? product.Price) >= 1_000_000m && ((product.DiscountPrice ?? product.SalePrice) ?? product.Price) < 2_000_000m) ||
            (selectedValues.Contains("2-5") && ((product.DiscountPrice ?? product.SalePrice) ?? product.Price) >= 2_000_000m && ((product.DiscountPrice ?? product.SalePrice) ?? product.Price) < 5_000_000m) ||
            (selectedValues.Contains("5-10") && ((product.DiscountPrice ?? product.SalePrice) ?? product.Price) >= 5_000_000m && ((product.DiscountPrice ?? product.SalePrice) ?? product.Price) < 10_000_000m) ||
            (selectedValues.Contains("10-20") && ((product.DiscountPrice ?? product.SalePrice) ?? product.Price) >= 10_000_000m && ((product.DiscountPrice ?? product.SalePrice) ?? product.Price) < 20_000_000m) ||
            (selectedValues.Contains("20-30") && ((product.DiscountPrice ?? product.SalePrice) ?? product.Price) >= 20_000_000m && ((product.DiscountPrice ?? product.SalePrice) ?? product.Price) < 30_000_000m) ||
            (selectedValues.Contains("30-50") && ((product.DiscountPrice ?? product.SalePrice) ?? product.Price) >= 30_000_000m && ((product.DiscountPrice ?? product.SalePrice) ?? product.Price) < 50_000_000m) ||
            (selectedValues.Contains("over-50") && ((product.DiscountPrice ?? product.SalePrice) ?? product.Price) >= 50_000_000m));
    }

    private static IEnumerable<Product> ApplySpecFilter(IEnumerable<Product> products, string[] selectedSpecs)
    {
        var selectedPairs = selectedSpecs
            .Select(ParseSpecValue)
            .Where(pair => pair is not null)
            .Select(pair => pair!.Value)
            .ToArray();

        if (selectedPairs.Length == 0)
            return products;

        return products.Where(product =>
        {
            var productSpecs = ProductSpecificationKeyValueHelper.ParseStored(product.Specifications);
            return selectedPairs.All(selected => productSpecs.Any(spec =>
                string.Equals(spec.Name?.Trim(), selected.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(spec.Value?.Trim(), selected.Value, StringComparison.OrdinalIgnoreCase)));
        });
    }

    private static (string Name, string Value)? ParseSpecValue(string raw)
    {
        var parts = raw.Split("::", 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            return null;

        return (parts[0], parts[1]);
    }
}
