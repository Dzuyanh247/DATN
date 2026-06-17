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
        string? typeSlug,
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
            Type = ResolveComponentType(type, typeSlug),
            ComponentTypes = Array.Empty<string>(),
            Specs = CleanSelections(specs),
            Cpu = CleanSelections(cpu),
            Ram = CleanSelections(ram),
            Gpu = CleanSelections(gpu)
        };
        vm.ComponentTypes = CleanSelections(componentTypes)
            .Concat(CleanSelections(string.IsNullOrWhiteSpace(vm.Type) ? null : new[] { vm.Type }))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        vm.ComponentTypeLabel = string.IsNullOrWhiteSpace(vm.Type) ? null : GetComponentTypeLabel(vm.Type);
        vm.TypeSlug = string.IsNullOrWhiteSpace(vm.Type) ? null : GetComponentTypeSlug(vm.Type);

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

        vm.IsComponentListing = await IsComponentListingAsync(vm.Type, vm.CategorySlug, vm.CategoryId);
        if (!vm.IsComponentListing)
        {
            vm.Brands = Array.Empty<string>();
            vm.ComponentTypes = Array.Empty<string>();
        }

        if (vm.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == vm.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(vm.Type))
        {
            var scopedComponentType = ComponentTypes.Normalize(vm.Type);
            query = ApplyComponentTypeQuery(await ApplyComponentListingScopeAsync(query), new[] { scopedComponentType });
        }
        else if (vm.IsComponentListing)
        {
            query = await ApplyComponentListingScopeAsync(query);
        }

        query = query.Where(p => p.IsActive);

        var categoryProducts = await query
            .ToListAsync();
        var parsedFacets = categoryProducts.ToDictionary(product => product.Id, ProductFilterFacetHelper.Parse);
        PopulateFilterOptions(vm, categoryProducts, parsedFacets);

        if (vm.ComponentTypes.Length > 0)
            query = ApplyComponentTypeQuery(query, vm.ComponentTypes);
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
        return Index(keyword: null, categoryId: null, categorySlug: null, brand: brand, type: type, typeSlug: null, minPrice: null, maxPrice: null, priceRanges: null, brands: null, componentTypes: null, specs: null, cpu: null, ram: null, gpu: null, sort: null);
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


    private static string? ResolveComponentType(string? type, string? typeSlug)
    {
        if (!string.IsNullOrWhiteSpace(type)) return ComponentTypes.Normalize(type);
        if (string.IsNullOrWhiteSpace(typeSlug)) return null;

        var normalizedSlug = typeSlug.Trim().ToLowerInvariant();
        return ComponentTypes.Slugs.FirstOrDefault(item => item.Value == normalizedSlug).Key;
    }

    private static string GetComponentTypeSlug(string type) => ComponentTypes.GetSlug(type);

    private static string GetComponentTypeLabel(string type) => ComponentTypes.GetLabel(type);

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
            .Where(ProductFilterFacetHelper.IsRenderableFilterOption)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();


    private async Task<IQueryable<Product>> ApplyComponentListingScopeAsync(IQueryable<Product> query)
    {
        var componentCategoryIds = await _db.Categories.AsNoTracking()
            .Where(category => category.Name.Contains("Linh kiện") || category.Name.Contains("Component"))
            .Select(category => category.Id)
            .ToListAsync();

        return componentCategoryIds.Count == 0
            ? query.Where(product => product.ProductType == ProductKinds.Component)
            : query.Where(product => product.ProductType == ProductKinds.Component || componentCategoryIds.Contains(product.CategoryId));
    }

    private async Task<bool> IsComponentListingAsync(string? type, string? categorySlug, int? categoryId)
    {
        if (!string.IsNullOrWhiteSpace(type)) return true;
        var slug = categorySlug?.Trim().ToLowerInvariant();
        if (slug is "linh-kien" or "linh-kien-may-tinh" or "components" or "component") return true;
        if (!categoryId.HasValue) return false;
        var categoryName = await _db.Categories
            .Where(category => category.Id == categoryId.Value)
            .Select(category => category.Name)
            .FirstOrDefaultAsync();
        return categoryName?.Contains("linh kiện", StringComparison.OrdinalIgnoreCase) == true
            || categoryName?.Contains("component", StringComparison.OrdinalIgnoreCase) == true;
    }

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

        vm.ComponentTypeGroups = vm.IsComponentListing ? BuildComponentTypeGroups(products, vm.Type) : new List<ProductFilterGroupVm>();
        if (!vm.IsComponentListing) vm.BrandOptions.Clear();
        vm.SpecFilterGroups = string.IsNullOrWhiteSpace(vm.Type) ? new List<ProductSpecFilterGroupVm>() : BuildSpecFilterGroups(products, vm.Type);

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

    private static List<ProductFilterGroupVm> BuildComponentTypeGroups(IReadOnlyCollection<Product> products, string? currentType)
    {
        var counts = products
            .Where(product => !string.IsNullOrWhiteSpace(product.ComponentType))
            .GroupBy(product => ComponentTypes.Normalize(product.ComponentType), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        var groups = new List<ProductFilterGroupVm>
        {
            new()
            {
                Title = "Linh kiện máy tính",
                Options = BuildComponentOptions(counts, currentType,
                    ("CPU", "CPU"),
                    ("Mainboard", "Mainboard - Bo mạch chủ"),
                    ("RAM", "RAM"),
                    ("VGA", "VGA - Card màn hình"),
                    ("Storage", "Storage - SSD/HDD"),
                    ("Cooler", "Tản nhiệt"),
                    ("Case", "Vỏ case"),
                    ("PSU", "Nguồn (PSU)"))
            },
            new()
            {
                Title = "Ngoại vi",
                Options = BuildComponentOptions(counts, currentType,
                    ("Monitor", "Monitor - Màn hình"),
                    ("Keyboard", "Keyboard - Bàn phím"),
                    ("Mouse", "Mouse - Chuột"),
                    ("Headphone", "Headphone - Tai nghe"),
                    ("MonitorArm", "MonitorArm - Giá treo màn hình"),
                    ("Other", "Other - Khác"))
            }
        };

        return groups.Where(group => group.Options.Count > 0).ToList();
    }

    private static List<ProductFilterOptionVm> BuildComponentOptions(Dictionary<string, int> counts, string? currentType, params (string Value, string Label)[] options) =>
        options
            .Select(option => new ProductFilterOptionVm
            {
                Value = option.Value,
                Label = option.Label,
                Count = GetComponentTypeCount(counts, option.Value),
                Url = $"/linh-kien/{GetComponentTypeSlug(option.Value)}"
            })
            .Where(option => option.Count > 0 || string.Equals(option.Value, currentType, StringComparison.OrdinalIgnoreCase))
            .ToList();

    private static int GetComponentTypeCount(Dictionary<string, int> counts, string type)
    {
        return counts.GetValueOrDefault(ComponentTypes.Normalize(type));
    }

    private static List<ProductSpecFilterGroupVm> BuildSpecFilterGroups(IEnumerable<Product> products, string componentType) =>
        products
            .SelectMany(product => ProductSpecificationKeyValueHelper.ParseStored(product.Specifications)
                .Where(spec => spec.IsFilterable && !string.IsNullOrWhiteSpace(spec.Name) && !string.IsNullOrWhiteSpace(spec.Value))
                .Select(spec => new { product.Id, Name = spec.Name.Trim(), Value = spec.Value.Trim() }))
            .Where(spec => ProductFilterFacetHelper.IsRenderableFilterOption(spec.Name)
                && ProductFilterFacetHelper.IsRenderableFilterOption(spec.Value)
                && IsSpecAllowedForComponent(componentType, spec.Name))
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

    private static bool IsSpecAllowedForComponent(string componentType, string specName)
    {
        var allowedKeys = GetAllowedSpecKeys(componentType);
        if (allowedKeys.Count == 0) return true;
        return allowedKeys.Contains(NormalizeSpecKey(specName));
    }

    private static HashSet<string> GetAllowedSpecKeys(string componentType)
    {
        var keys = componentType.Trim().ToLowerInvariant() switch
        {
            "cpu" => new[] { "thuonghieu", "loaicpu", "socket", "thehecpu", "thehe", "tengoi", "tenthehe", "sonhan", "soluong", "tdp", "hotrobonho", "hotroram", "tan-nhiet", "tannhiet", "dongcpu", "series" },
            "ram" => new[] { "dungluong", "busram", "bus", "loairam", "chuanram" },
            "vga" => new[] { "chipset", "dungluongvram", "vram", "seriesgpu", "series" },
            "mainboard" => new[] { "socket", "chipset", "formfactor", "ramhotro", "chuanram" },
            "ssd" or "hdd" or "storage" => new[] { "dungluong", "chuanocung", "giaotiep", "loaiocung" },
            "psu" => new[] { "congsuat", "chuannnguon", "chuannguon", "hieusuat80plus", "80plus" },
            "monitor" => new[] { "kichthuoc", "tansoquet", "dophangiai", "tamnen" },
            "keyboard" => new[] { "loaiswitch", "switch", "layout", "ketnoi" },
            "mouse" => new[] { "dpi", "ketnoi", "loaicambien", "cambien" },
            "headphone" => new[] { "ketnoi", "kieutainghe", "amthanh" },
            "case" => new[] { "formmainhotro", "formfactor", "kichthuoccase", "kichthuoc" },
            "cooler" => new[] { "loaitan", "sockethotro", "socket" },
            _ => Array.Empty<string>()
        };
        return keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeSpecKey(string value)
    {
        var normalized = value.Trim().ToLowerInvariant()
            .Replace("đ", "d");
        var chars = normalized.Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .Where(char.IsLetterOrDigit)
            .ToArray();
        return new string(chars);
    }

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

    private static IQueryable<Product> ApplyComponentTypeQuery(IQueryable<Product> query, string[] componentTypes)
    {
        if (componentTypes.Length == 0) return query;
        var expandedTypes = componentTypes
            .Select(ComponentTypes.Normalize)
            .SelectMany(GetComponentTypeAliases)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return query.Where(product => product.ComponentType != null && expandedTypes.Contains(product.ComponentType));
    }


    private static string[] GetComponentTypeAliases(string type) => ComponentTypes.Normalize(type) switch
    {
        ComponentTypes.CPU => new[] { "CPU", "CPU - Bộ vi xử lý", "Bộ vi xử lý", "cpu" },
        ComponentTypes.Mainboard => new[] { "Mainboard", "MAINBOARD", "Mainboard - Bo mạch chủ", "Bo mạch chủ", "Motherboard", "mainboard" },
        ComponentTypes.RAM => new[] { "RAM", "Ram", "Bộ nhớ trong" },
        ComponentTypes.VGA => new[] { "VGA", "GPU", "VGA - Card màn hình", "Card màn hình" },
        ComponentTypes.Storage => new[] { "Storage", "SSD", "HDD", "SSD/HDD", "Ổ cứng SSD/HDD", "Ổ cứng" },
        ComponentTypes.PSU => new[] { "PSU", "PSU - Nguồn máy tính", "Nguồn máy tính" },
        ComponentTypes.Case => new[] { "Case", "Case - Vỏ case", "Vỏ case" },
        ComponentTypes.Cooler => new[] { "Cooler", "Cooler - Tản nhiệt", "Tản nhiệt" },
        ComponentTypes.Monitor => new[] { "Monitor", "Monitor - Màn hình", "Màn hình" },
        ComponentTypes.Keyboard => new[] { "Keyboard", "Keyboard - Bàn phím", "Bàn phím" },
        ComponentTypes.Mouse => new[] { "Mouse", "Mouse - Chuột", "Chuột" },
        ComponentTypes.Headphone => new[] { "Headphone", "Headphone - Tai nghe", "Headset", "Tai nghe" },
        ComponentTypes.MonitorArm => new[] { "MonitorArm", "MonitorArm - Giá treo màn hình", "Giá treo màn hình" },
        _ => new[] { ComponentTypes.Other, "Other", "Khác" }
    };

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

        var selectedByName = selectedPairs
            .GroupBy(pair => pair.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return products.Where(product =>
        {
            var productSpecs = ProductSpecificationKeyValueHelper.ParseStored(product.Specifications);
            return selectedByName.All(group => productSpecs.Any(spec =>
                string.Equals(spec.Name?.Trim(), group.Key, StringComparison.OrdinalIgnoreCase) &&
                group.Any(selected => string.Equals(spec.Value?.Trim(), selected.Value, StringComparison.OrdinalIgnoreCase))));
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
