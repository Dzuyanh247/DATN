using System.Linq.Expressions;
using Datn.PcStore.Data;
using Datn.PcStore.Helpers;
using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

public class ProductsController : Controller
{
    private readonly ApplicationDbContext _db;
    public ProductsController(ApplicationDbContext db) => _db = db;

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

        var query = _db.Products.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(vm.Keyword))
            query = query.Where(p => p.Name.Contains(vm.Keyword));
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

        var facetProducts = await query.AsNoTracking().ToListAsync();
        var parsedFacets = facetProducts.ToDictionary(product => product.Id, ProductFilterFacetHelper.Parse);
        PopulateFilterOptions(vm, facetProducts, parsedFacets);

        if (vm.PriceRanges.Length > 0)
            query = ApplyPriceRangeFilter(query, vm.PriceRanges);
        if (vm.Brands.Length > 0)
            query = query.Where(product => vm.Brands.Contains(product.Brand));

        var matchingIds = GetMatchingParsedFacetIds(facetProducts, parsedFacets, vm);
        if (matchingIds is not null)
            query = query.Where(product => matchingIds.Contains(product.Id));

        vm.Categories = await _db.Categories.OrderBy(category => category.Name).ToListAsync();
        vm.Products = await query
            .Include(p => p.ProductImages)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(vm);
    }

    public async Task<IActionResult> Detail(int id)
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

        return View(product);
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
            .Where(product => !string.IsNullOrWhiteSpace(product.Brand))
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

    private static IQueryable<Product> ApplyPriceRangeFilter(IQueryable<Product> query, string[] selectedValues)
    {
        var selectedRanges = ProductFilterFacetHelper.PriceRanges
            .Where(range => selectedValues.Contains(range.Value, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (selectedRanges.Count == 0)
            return query;

        var product = Expression.Parameter(typeof(Product), "product");
        var discountPrice = Expression.Property(product, nameof(Product.DiscountPrice));
        var salePrice = Expression.Property(product, nameof(Product.SalePrice));
        var price = Expression.Property(product, nameof(Product.Price));
        var effectivePrice = Expression.Coalesce(Expression.Coalesce(discountPrice, salePrice), price);
        Expression? rangeExpression = null;

        foreach (var range in selectedRanges)
        {
            Expression current = Expression.Constant(true);
            if (range.MinPrice.HasValue)
                current = Expression.AndAlso(current, Expression.GreaterThanOrEqual(effectivePrice, Expression.Constant(range.MinPrice.Value)));
            if (range.MaxPrice.HasValue)
                current = Expression.AndAlso(current, Expression.LessThan(effectivePrice, Expression.Constant(range.MaxPrice.Value)));
            rangeExpression = rangeExpression is null ? current : Expression.OrElse(rangeExpression, current);
        }

        var predicate = Expression.Lambda<Func<Product, bool>>(rangeExpression!, product);
        return query.Where(predicate);
    }
}
