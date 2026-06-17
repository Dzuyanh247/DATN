using Datn.PcStore.Models;

namespace Datn.PcStore.ViewModels;

public class ProductFilterVm
{
    public string? Keyword { get; set; }
    public int? CategoryId { get; set; }
    public string? CategorySlug { get; set; }
    public string? Brand { get; set; }
    public string? Type { get; set; }
    public string? ComponentTypeLabel { get; set; }
    public string? TypeSlug { get; set; }
    public bool HasScopedComponentType => !string.IsNullOrWhiteSpace(Type);
    public string? Sort { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string[] PriceRanges { get; set; } = Array.Empty<string>();
    public string[] Brands { get; set; } = Array.Empty<string>();
    public string[] ComponentTypes { get; set; } = Array.Empty<string>();
    public string[] Specs { get; set; } = Array.Empty<string>();
    public string[] Cpu { get; set; } = Array.Empty<string>();
    public string[] Ram { get; set; } = Array.Empty<string>();
    public string[] Gpu { get; set; } = Array.Empty<string>();
    public List<Category> Categories { get; set; } = new();
    public List<Product> Products { get; set; } = new();
    public List<ProductFilterOptionVm> PriceRangeOptions { get; set; } = new();
    public List<ProductFilterOptionVm> BrandOptions { get; set; } = new();
    public List<ProductFilterGroupVm> ComponentTypeGroups { get; set; } = new();
    public List<ProductSpecFilterGroupVm> SpecFilterGroups { get; set; } = new();
    public List<ProductFilterOptionVm> CpuOptions { get; set; } = new();
    public List<ProductFilterOptionVm> RamOptions { get; set; } = new();
    public List<ProductFilterOptionVm> GpuOptions { get; set; } = new();
    public bool IsEquivalentSearch { get; set; }
    public bool IsComponentListing { get; set; }

    public bool HasSidebarFilters => PriceRanges.Length > 0 || (IsComponentListing && Brands.Length > 0) || (IsComponentListing && ComponentTypes.Length > 0) || Specs.Length > 0 || Cpu.Length > 0 || Ram.Length > 0 || Gpu.Length > 0;
    public bool HasKeyword => !string.IsNullOrWhiteSpace(Keyword);
}

public class ProductFilterOptionVm
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public string? Url { get; set; }
}

public class ProductFilterGroupVm
{
    public string Title { get; set; } = string.Empty;
    public List<ProductFilterOptionVm> Options { get; set; } = new();
}

public class ProductSpecFilterGroupVm
{
    public string Name { get; set; } = string.Empty;
    public List<ProductFilterOptionVm> Options { get; set; } = new();
}
