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
    private readonly ILogger<ProductsController> _logger;
    private readonly ISearchKeywordService _searchKeywordService;

    public ProductsController(ApplicationDbContext db, IProductReviewService reviewService, ILogger<ProductsController> logger, ISearchKeywordService searchKeywordService)
    {
        _db = db;
        _reviewService = reviewService;
        _logger = logger;
        _searchKeywordService = searchKeywordService;
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
        string[]? componentFamilies,
        string[]? specs,
        string[]? cpu,
        string[]? ram,
        string[]? gpu,
        string[]? storage,
        string[]? mainboard,
        string[]? psu,
        string[]? @case,
        string[]? cooling,
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
            ComponentFamilies = CleanSelections(componentFamilies),
            Specs = CleanSelections(specs),
            Cpu = CleanSelections(cpu),
            Ram = CleanSelections(ram),
            Gpu = CleanSelections(gpu),
            Storage = CleanSelections(storage),
            Mainboard = CleanSelections(mainboard),
            Psu = CleanSelections(psu),
            Case = CleanSelections(@case),
            Cooling = CleanSelections(cooling)
        };
        vm.ComponentTypes = CleanSelections(componentTypes)
            .Concat(CleanSelections(string.IsNullOrWhiteSpace(vm.Type) ? null : new[] { vm.Type }))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        vm.ComponentTypeLabel = string.IsNullOrWhiteSpace(vm.Type) ? null : GetComponentTypeLabel(vm.Type);
        vm.TypeSlug = string.IsNullOrWhiteSpace(vm.Type) ? null : GetComponentTypeSlug(vm.Type);

        vm.Keyword = vm.Keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(vm.Keyword))
        {
            await _searchKeywordService.TrackSearchAsync(vm.Keyword);
        }

        var query = _db.Products.Include(p => p.Category).AsNoTracking().AsQueryable();
        if (!vm.CategoryId.HasValue && !string.IsNullOrWhiteSpace(vm.CategorySlug))
        {
            var normalizedSlug = NormalizeSlug(vm.CategorySlug);
            vm.CategoryId = (await _db.Categories
                    .AsNoTracking()
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync())
                .Where(c => string.Equals(NormalizeSlug(c.Name), normalizedSlug, StringComparison.OrdinalIgnoreCase))
                .Select(c => (int?)c.Id)
                .FirstOrDefault();
        }

        var selectedCategory = vm.CategoryId.HasValue
            ? await _db.Categories
                .AsNoTracking()
                .Where(category => category.Id == vm.CategoryId.Value)
                .Select(category => new
                {
                    category.Id,
                    category.Name,
                    Slug = NormalizeSlug(category.Name)
                })
                .FirstOrDefaultAsync()
            : null;

        _logger.LogWarning(
            "DEBUG CATEGORY => categoryId={CategoryId}, categoryName={CategoryName}, requestedCategorySlug={RequestedCategorySlug}, loadedCategorySlug={LoadedCategorySlug}",
            vm.CategoryId,
            selectedCategory?.Name,
            vm.CategorySlug,
            selectedCategory?.Slug);

        if (selectedCategory is not null)
        {
            vm.CategorySlug = selectedCategory.Slug;
        }

        vm.IsComponentListing = await IsComponentListingAsync(vm.Type, vm.CategorySlug, vm.CategoryId);
        if (!vm.IsComponentListing)
        {
            vm.ComponentTypes = Array.Empty<string>();
        }

        if (vm.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == vm.CategoryId.Value);

        if (vm.IsComponentListing)
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
        if (vm.ComponentFamilies.Length > 0)
            filteredProducts = filteredProducts.Where(product => vm.ComponentFamilies.Any(family => ProductMatchesFamily(product, family)));

        var matchingIds = GetMatchingParsedFacetIds(filteredFacetProducts, filteredParsedFacets, vm);
        if (matchingIds is not null)
            filteredProducts = filteredProducts.Where(product => matchingIds.Contains(product.Id));
        if (vm.Specs.Length > 0)
            filteredProducts = ApplySpecFilter(filteredProducts, vm.Specs);

        vm.Categories = await _db.Categories.OrderBy(category => category.Name).ToListAsync();
        vm.Products = ApplySort(filteredProducts, vm.Sort).ToList();

        SetFilterRouteUrls(vm);

        _logger.LogWarning(
            "DEBUG FILTER => categoryId={CategoryId}, categorySlug={CategorySlug}, type={Type}, typeSlug={TypeSlug}, isComponentListing={IsComponentListing}, filterActionUrl={FilterActionUrl}, clearFilterUrl={ClearFilterUrl}",
            vm.CategoryId,
            vm.CategorySlug,
            vm.Type,
            vm.TypeSlug,
            vm.IsComponentListing,
            vm.FilterActionUrl,
            vm.ClearFilterUrl);

        LogFilterDebug(vm, categoryProducts.Count, vm.Products.Count);

        return View(vm);
    }

    public Task<IActionResult> Category(string? type, string? brand)
    {
        return Index(keyword: null, categoryId: null, categorySlug: null, brand: brand, type: type, typeSlug: null, minPrice: null, maxPrice: null, priceRanges: null, brands: null, componentTypes: null, componentFamilies: null, specs: null, cpu: null, ram: null, gpu: null, storage: null, mainboard: null, psu: null, @case: null, cooling: null, sort: null);
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

        var accessoryTypeAliases = ComponentTypes.GetAliases(ComponentTypes.Monitor)
            .Concat(ComponentTypes.GetAliases(ComponentTypes.Keyboard))
            .Concat(ComponentTypes.GetAliases(ComponentTypes.Mouse))
            .Concat(ComponentTypes.GetAliases(ComponentTypes.Headphone))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var accessoryProducts = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.ProductImages.OrderBy(x => x.SortOrder))
            .Where(p => p.IsActive
                && p.IsInStock
                && p.StockQuantity > 0
                && p.ProductType == ProductKinds.Component
                && accessoryTypeAliases.Contains(p.ComponentType))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var vm = new ProductDetailViewModel
        {
            Product = product,
            Monitors = accessoryProducts.Where(p => ComponentTypes.Normalize(p.ComponentType) == ComponentTypes.Monitor).ToList(),
            Keyboards = accessoryProducts.Where(p => ComponentTypes.Normalize(p.ComponentType) == ComponentTypes.Keyboard).ToList(),
            Mice = accessoryProducts.Where(p => ComponentTypes.Normalize(p.ComponentType) == ComponentTypes.Mouse).ToList(),
            Headsets = accessoryProducts.Where(p => ComponentTypes.Normalize(p.ComponentType) == ComponentTypes.Headphone).ToList()
        };

        return View(vm);
    }


    private void SetFilterRouteUrls(ProductFilterVm vm)
    {
        vm.CurrentPath = Request.Path.Value ?? string.Empty;
        var isComponentFilterRoute = IsComponentFilterRoute(vm.CurrentPath);
        var routeBasedFilterActionUrl = isComponentFilterRoute ? BuildComponentFilterPath(vm) : "/Products";

        if (vm.IsComponentListing)
        {
            vm.FilterActionUrl = vm.HasScopedComponentType
                ? $"/linh-kien/{vm.TypeSlug}"
                : "/linh-kien";

            vm.ClearFilterUrl = "/linh-kien";
        }
        else
        {
            vm.FilterActionUrl = "/Products";
            vm.ClearFilterUrl = Url.Action(nameof(Index), "Products", new
            {
                categoryId = vm.CategoryId,
                categorySlug = vm.CategorySlug
            }) ?? "/Products";
        }

        _logger.LogWarning(
            "DEBUG FILTER ROUTE => isComponentFilterRoute={IsComponentFilterRoute}, routeBasedFilterActionUrl={RouteBasedFilterActionUrl}, finalFilterActionUrl={FinalFilterActionUrl}, finalClearFilterUrl={FinalClearFilterUrl}",
            isComponentFilterRoute,
            routeBasedFilterActionUrl,
            vm.FilterActionUrl,
            vm.ClearFilterUrl);
    }

    private static bool IsComponentFilterRoute(string currentPath) =>
        currentPath.StartsWith("/linh-kien", StringComparison.OrdinalIgnoreCase);

    private static string BuildComponentFilterPath(ProductFilterVm vm) =>
        vm.HasScopedComponentType ? $"/linh-kien/{vm.TypeSlug}" : "/linh-kien";

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
            ? query.Where(product => product.ProductType == ProductKinds.Component || (product.ProductType != null && product.ProductType.Contains("Linh")))
            : query.Where(product => product.ProductType == ProductKinds.Component || (product.ProductType != null && product.ProductType.Contains("Linh") && componentCategoryIds.Contains(product.CategoryId)));
    }

    private async Task<bool> IsComponentListingAsync(string? type, string? categorySlug, int? categoryId)
    {
        if (!string.IsNullOrWhiteSpace(type)) return true;

        var normalizedSlug = NormalizeSlug(categorySlug);
        if (IsComponentCategorySlug(normalizedSlug)) return true;

        if (!categoryId.HasValue) return false;

        var category = await _db.Categories
            .AsNoTracking()
            .Where(item => item.Id == categoryId.Value)
            .Select(item => new { item.Name })
            .FirstOrDefaultAsync();

        if (category is null) return false;

        var normalizedCategoryName = NormalizeSearchValue(category.Name);
        if (normalizedCategoryName.Contains("linh kien", StringComparison.OrdinalIgnoreCase)
            || normalizedCategoryName.Contains("component", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IsComponentCategorySlug(NormalizeSlug(category.Name));
    }

    private static bool IsComponentCategorySlug(string? slug) =>
        slug is "linh-kien" or "linh-kien-may-tinh" or "components" or "component";

    private static string? NormalizeSlug(string? value)
    {
        var normalized = NormalizeSearchValue(value);
        if (string.IsNullOrWhiteSpace(normalized)) return null;

        var chars = normalized
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();
        return string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string NormalizeSearchValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var normalized = value.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(System.Text.NormalizationForm.FormD);
        var chars = normalized
            .Where(character => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray();
        return new string(chars).Normalize(System.Text.NormalizationForm.FormC);
    }

    private void LogFilterDebug(ProductFilterVm vm, int baseProductCount, int filteredProductCount)
    {
        _logger.LogDebug(
            "Products filter: currentPath={CurrentPath}, filterActionUrl={FilterActionUrl}, clearFilterUrl={ClearFilterUrl}, categoryId={CategoryId}, categorySlug={CategorySlug}, isComponentListing={IsComponentListing}, type={Type}, baseProducts={BaseProducts}, selectedFilters={SelectedFilters}, filteredProducts={FilteredProducts}, facets price={PriceFacetCount}, brand={BrandFacetCount}, cpu={CpuFacetCount}, ram={RamFacetCount}, gpu={GpuFacetCount}, storage={StorageFacetCount}, specGroups={SpecGroupCount}",
            vm.CurrentPath,
            vm.FilterActionUrl,
            vm.ClearFilterUrl,
            vm.CategoryId,
            vm.CategorySlug,
            vm.IsComponentListing,
            vm.Type,
            baseProductCount,
            string.Join("; ", BuildSelectedFilterDebugValues(vm)),
            filteredProductCount,
            vm.PriceRangeOptions.Count,
            vm.BrandOptions.Count,
            vm.CpuOptions.Count,
            vm.RamOptions.Count,
            vm.GpuOptions.Count,
            vm.StorageOptions.Count,
            vm.SpecFilterGroups.Count);
    }

    private static IEnumerable<string> BuildSelectedFilterDebugValues(ProductFilterVm vm)
    {
        if (vm.PriceRanges.Length > 0) yield return $"price=[{string.Join(',', vm.PriceRanges)}]";
        if (vm.Brands.Length > 0) yield return $"brands=[{string.Join(',', vm.Brands)}]";
        if (vm.ComponentTypes.Length > 0) yield return $"componentTypes=[{string.Join(',', vm.ComponentTypes)}]";
        if (vm.ComponentFamilies.Length > 0) yield return $"componentFamilies=[{string.Join(',', vm.ComponentFamilies)}]";
        if (vm.Cpu.Length > 0) yield return $"cpu=[{string.Join(',', vm.Cpu)}]";
        if (vm.Ram.Length > 0) yield return $"ram=[{string.Join(',', vm.Ram)}]";
        if (vm.Gpu.Length > 0) yield return $"gpu=[{string.Join(',', vm.Gpu)}]";
        if (vm.Storage.Length > 0) yield return $"storage=[{string.Join(',', vm.Storage)}]";
        if (vm.Mainboard.Length > 0) yield return $"mainboard=[{string.Join(',', vm.Mainboard)}]";
        if (vm.Psu.Length > 0) yield return $"psu=[{string.Join(',', vm.Psu)}]";
        if (vm.Case.Length > 0) yield return $"case=[{string.Join(',', vm.Case)}]";
        if (vm.Cooling.Length > 0) yield return $"cooling=[{string.Join(',', vm.Cooling)}]";
        if (vm.Specs.Length > 0) yield return $"specs=[{string.Join(',', vm.Specs)}]";
    }

    private static void PopulateFilterOptions(
        ProductFilterVm vm,
        IReadOnlyCollection<Product> products,
        IReadOnlyDictionary<int, ProductParsedFacets> parsedFacets)
    {
        var scopedProducts = vm.IsComponentListing && !string.IsNullOrWhiteSpace(vm.Type)
            ? products.Where(product => ComponentTypes.GetAliases(vm.Type).Contains(product.ComponentType, StringComparer.OrdinalIgnoreCase)).ToList()
            : products.ToList();

        vm.PriceRangeOptions = ProductFilterFacetHelper.PriceRanges
            .Select(range => new ProductFilterOptionVm
            {
                Value = range.Value,
                Label = range.Label,
                Count = scopedProducts.Count(product => ProductFilterFacetHelper.IsInPriceRange(
                    ProductFilterFacetHelper.GetEffectivePrice(product), range))
            })
            .ToList();

        vm.BrandOptions = scopedProducts
            .Where(product => !string.IsNullOrWhiteSpace(product.Brand) && !string.Equals(product.Brand.Trim(), "N/A", StringComparison.OrdinalIgnoreCase))
            .GroupBy(product => product.Brand!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new ProductFilterOptionVm { Value = group.Key, Label = group.Key, Count = group.Count() })
            .OrderBy(option => option.Label)
            .ToList();

        vm.ComponentTypeGroups = vm.IsComponentListing && string.IsNullOrWhiteSpace(vm.Type) ? BuildComponentTypeGroups(products, vm.Type) : new List<ProductFilterGroupVm>();
        vm.ComponentFamilyOptions = vm.IsComponentListing && !string.IsNullOrWhiteSpace(vm.Type) ? BuildComponentFamilyOptions(scopedProducts, vm.Type) : new List<ProductFilterOptionVm>();
        vm.SpecFilterGroups = string.IsNullOrWhiteSpace(vm.Type) ? new List<ProductSpecFilterGroupVm>() : BuildSpecFilterGroups(scopedProducts, vm.Type);

        var showPcFacets = !vm.IsComponentListing;
        if (showPcFacets)
        {
            vm.CpuOptions = BuildParsedOptions(scopedProducts, parsedFacets, facets => facets.Cpu)
                .OrderBy(option => GetCpuSortOrder(option.Value))
                .ThenBy(option => option.Label)
                .ToList();
            vm.RamOptions = BuildParsedOptions(scopedProducts, parsedFacets, facets => facets.Ram)
                .OrderBy(option => ParseRamCapacity(option.Value))
                .ToList();
            vm.GpuOptions = BuildParsedOptions(scopedProducts, parsedFacets, facets => facets.Gpu)
                .OrderBy(option => GetGpuSortOrder(option.Value))
                .ThenBy(option => option.Label)
                .ToList();
            // PC listings intentionally whitelist only the friendly top-level facets:
            // price, brand, CPU series, RAM capacity, GPU series, and storage capacity.
            // Detailed component facets such as mainboard, PSU, case, and cooling are
            // omitted to avoid exposing long component names as noisy filter options.
            vm.StorageOptions = BuildParsedOptions(scopedProducts, parsedFacets, facets => facets.Storage)
                .OrderBy(option => ParseStorageCapacity(option.Value))
                .ToList();
            vm.MainboardOptions.Clear();
            vm.PsuOptions.Clear();
            vm.CaseOptions.Clear();
            vm.CoolingOptions.Clear();
        }
    }


    private static List<ProductFilterOptionVm> BuildComponentFamilyOptions(IEnumerable<Product> products, string componentType)
    {
        var normalizedType = ComponentTypes.Normalize(componentType);
        var families = GetComponentFamilyLabels(normalizedType);
        if (families.Length == 0)
            return new List<ProductFilterOptionVm>();

        var productList = products.ToList();
        return families
            .Select(family => new ProductFilterOptionVm
            {
                Value = family.BrandValue,
                Label = family.Label,
                Count = productList.Count(product => ProductMatchesFamily(product, family.BrandValue))
            })
            .Where(option => option.Count > 0)
            .ToList();
    }

    private static (string BrandValue, string Label)[] GetComponentFamilyLabels(string componentType) => componentType switch
    {
        ComponentTypes.CPU => new[] { ("Intel", "CPU Intel"), ("AMD", "CPU AMD") },
        ComponentTypes.Mainboard => new[] { ("Intel", "Mainboard Intel"), ("AMD", "Mainboard AMD") },
        ComponentTypes.VGA => new[] { ("NVIDIA", "NVIDIA"), ("AMD", "AMD") },
        _ => Array.Empty<(string BrandValue, string Label)>()
    };

    private static bool ProductMatchesFamily(Product product, string family)
    {
        var haystack = string.Join(' ', product.Brand, product.Name, product.ShortDescription, product.Description, product.DetailDescription, product.Specifications);
        if (string.Equals(family, "NVIDIA", StringComparison.OrdinalIgnoreCase))
            return haystack.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) || haystack.Contains("RTX", StringComparison.OrdinalIgnoreCase) || haystack.Contains("GTX", StringComparison.OrdinalIgnoreCase);

        return haystack.Contains(family, StringComparison.OrdinalIgnoreCase);
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
                    ComponentTypes.CPU, ComponentTypes.Mainboard, ComponentTypes.RAM, ComponentTypes.VGA,
                    ComponentTypes.MonitorArm, ComponentTypes.Storage, ComponentTypes.Cooler, ComponentTypes.Case, ComponentTypes.PSU)
            }
        };

        return groups.Where(group => group.Options.Count > 0).ToList();
    }

    private static List<ProductFilterOptionVm> BuildComponentOptions(Dictionary<string, int> counts, string? currentType, params string[] options) =>
        options
            .Select(option => new ProductFilterOptionVm
            {
                Value = option,
                Label = ComponentTypes.GetLabel(option),
                Count = GetComponentTypeCount(counts, option),
                Url = $"/linh-kien/{GetComponentTypeSlug(option)}"
            })
            .ToList();

    private static int GetComponentTypeCount(Dictionary<string, int> counts, string type)
    {
        return counts.GetValueOrDefault(ComponentTypes.Normalize(type));
    }

    private static List<ProductSpecFilterGroupVm> BuildSpecFilterGroups(IEnumerable<Product> products, string componentType) =>
        products
            .SelectMany(product => ProductSpecificationKeyValueHelper.ParseStored(product.Specifications)
                .Where(spec => spec.IsFilterable && !string.IsNullOrWhiteSpace(spec.Name) && !string.IsNullOrWhiteSpace(spec.Value))
                .Select(spec => new { product.Id, Name = spec.Name?.Trim() ?? string.Empty, Value = spec.Value?.Trim() ?? string.Empty }))
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
        if (allowedKeys.Count == 0) return false;
        return allowedKeys.Contains(NormalizeSpecKey(specName));
    }

    private static HashSet<string> GetAllowedSpecKeys(string componentType)
    {
        var keys = componentType.Trim().ToLowerInvariant() switch
        {
            "cpu" => new[] { "hangcpu", "hang", "thuonghieu", "socket", "sonhan", "nhan", "soluong", "luong", "tdp", "thehecpu", "thehe" },
            "ram" => new[] { "dungluong", "busram", "bus", "loairam", "chuanram" },
            "vga" => new[] { "chipset", "dungluongvram", "vram", "seriesgpu", "series" },
            "mainboard" => new[] { "hang", "thuonghieu", "socket", "chipset" },
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


    private static int GetCpuSortOrder(string value)
    {
        var order = new[] { "AMD Ryzen 3", "AMD Ryzen 5", "AMD Ryzen 7", "AMD Ryzen 9", "Intel Core i3", "Intel Core i5", "Intel Core i7", "Intel Core i9", "Intel Core Ultra 5", "Intel Core Ultra 7", "Intel Core Ultra 9" };
        var index = Array.FindIndex(order, item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index : int.MaxValue;
    }

    private static int GetGpuSortOrder(string value)
    {
        var order = new[]
        {
            "NVIDIA RTX 3050", "NVIDIA RTX 3060", "NVIDIA RTX 4060", "NVIDIA RTX 4060 Ti",
            "NVIDIA RTX 4070", "NVIDIA RTX 4070 Ti", "NVIDIA RTX 4080", "NVIDIA RTX 4090",
            "NVIDIA RTX 5060", "NVIDIA RTX 5060 Ti", "NVIDIA RTX 5070", "NVIDIA RTX 5070 Ti",
            "NVIDIA RTX 5080", "NVIDIA RTX 5090", "AMD RX 7600", "AMD RX 7700 XT",
            "AMD RX 7800 XT", "AMD RX 7900 XT", "AMD RX 7900 XTX", "AMD RX 9060 XT"
        };
        var index = Array.FindIndex(order, item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index : int.MaxValue;
    }

    private static int ParseStorageCapacity(string value) =>
        value.Trim().ToUpperInvariant() switch
        {
            "256GB" => 256,
            "512GB" => 512,
            "1TB" => 1024,
            "2TB" => 2048,
            "4TB" => 4096,
            _ => int.MaxValue
        };

    private static HashSet<int>? GetMatchingParsedFacetIds(
        IEnumerable<Product> products,
        IReadOnlyDictionary<int, ProductParsedFacets> parsedFacets,
        ProductFilterVm vm)
    {
        if (vm.Cpu.Length == 0 && vm.Ram.Length == 0 && vm.Gpu.Length == 0 && vm.Storage.Length == 0 && vm.Mainboard.Length == 0 && vm.Psu.Length == 0 && vm.Case.Length == 0 && vm.Cooling.Length == 0)
            return null;

        return products
            .Where(product => MatchesAny(parsedFacets[product.Id].Cpu, vm.Cpu))
            .Where(product => MatchesAny(parsedFacets[product.Id].Ram, vm.Ram))
            .Where(product => MatchesAny(parsedFacets[product.Id].Gpu, vm.Gpu))
            .Where(product => MatchesAny(parsedFacets[product.Id].Storage, vm.Storage))
            .Where(product => MatchesAny(parsedFacets[product.Id].Mainboard, vm.Mainboard))
            .Where(product => MatchesAny(parsedFacets[product.Id].Psu, vm.Psu))
            .Where(product => MatchesAny(parsedFacets[product.Id].Case, vm.Case))
            .Where(product => MatchesAny(parsedFacets[product.Id].Cooling, vm.Cooling))
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
            .SelectMany(ComponentTypes.GetAliases)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return query.Where(product => product.ComponentType != null && expandedTypes.Contains(product.ComponentType));
    }

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
