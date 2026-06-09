using System.Text.RegularExpressions;
using Datn.PcStore.Models;

namespace Datn.PcStore.Helpers;

public static partial class ProductFilterFacetHelper
{
    private static readonly HashSet<string> SupportedRamCapacities =
        new(["8", "16", "32", "64"], StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlyList<PriceRangeDefinition> PriceRanges =
    [
        new("under-10", "Dưới 10 triệu", null, 10_000_000m),
        new("10-15", "10 - 15 triệu", 10_000_000m, 15_000_000m),
        new("15-20", "15 - 20 triệu", 15_000_000m, 20_000_000m),
        new("20-25", "20 - 25 triệu", 20_000_000m, 25_000_000m),
        new("25-35", "25 - 35 triệu", 25_000_000m, 35_000_000m),
        new("35-45", "35 - 45 triệu", 35_000_000m, 45_000_000m),
        new("45-60", "45 - 60 triệu", 45_000_000m, 60_000_000m),
        new("60-80", "60 - 80 triệu", 60_000_000m, 80_000_000m),
        new("80-100", "80 - 100 triệu", 80_000_000m, 100_000_000m),
        new("over-100", "Trên 100 triệu", 100_000_000m, null)
    ];

    public static decimal GetEffectivePrice(Product product) =>
        (product.DiscountPrice ?? product.SalePrice) ?? product.Price;

    public static bool IsInPriceRange(decimal price, PriceRangeDefinition range) =>
        (!range.MinPrice.HasValue || price >= range.MinPrice.Value) &&
        (!range.MaxPrice.HasValue || price < range.MaxPrice.Value);

    public static ProductParsedFacets Parse(Product product)
    {
        var sources = GetFacetSources(product);
        var cpu = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ram = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var gpu = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in sources)
        {
            cpu.UnionWith(ExtractMatches(source, CpuRegex(), match => NormalizeCpu(match.Value)));
            ram.UnionWith(ExtractRam(source));
            gpu.UnionWith(ExtractMatches(source, GpuRegex(), match => NormalizeGpu(match.Value)));
        }

        return new ProductParsedFacets(cpu, ram, gpu);
    }

    private static IReadOnlyCollection<string> GetFacetSources(Product product)
    {
        if (!string.IsNullOrWhiteSpace(product.Specifications))
        {
            var componentDescriptions = ProductComponentSpecHelper.ParseStored(product.Specifications)
                .Select(spec => spec.Description)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();

            if (componentDescriptions.Length > 0)
                return componentDescriptions;

            if (!string.Equals(product.Specifications.Trim(), "[]", StringComparison.Ordinal))
                return [product.Specifications];
        }

        return new[]
        {
            product.Name,
            product.ShortDescription,
            product.Description,
            product.DetailDescription
        }.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
    }

    private static HashSet<string> ExtractRam(string source)
    {
        // Avoid treating values such as "RTX 4060 8GB GDDR6" as system RAM.
        var sourceWithoutVram = GpuVramRegex().Replace(source, match => match.Groups[1].Value);
        return RamRegex().Matches(sourceWithoutVram)
            .Select(match => match.Groups[1].Value)
            .Where(SupportedRamCapacities.Contains)
            .Select(capacity => $"{capacity}GB")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> ExtractMatches(string source, Regex regex, Func<Match, string> selector) =>
        regex.Matches(source)
            .Select(selector)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static string NormalizeCpu(string value)
    {
        var normalized = WhitespaceRegex().Replace(value.Trim(), " ");
        var ryzenMatch = RyzenCpuRegex().Match(normalized);
        if (ryzenMatch.Success)
            return $"Ryzen {ryzenMatch.Groups[1].Value}";

        var coreMatch = CoreCpuRegex().Match(normalized);
        return coreMatch.Success ? $"Core i{coreMatch.Groups[1].Value}" : normalized;
    }

    private static string NormalizeGpu(string value)
    {
        var normalized = WhitespaceRegex().Replace(value.Replace('-', ' ').Trim(), " ").ToUpperInvariant();
        normalized = GpuVendorPrefixRegex().Replace(normalized, string.Empty);
        return normalized.Replace(" TI", " Ti", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"\b(?:AMD\s+)?Ryzen\s+[3579](?:\s+PRO)?\b|\b(?:Intel\s+)?Core\s+i[3579]\b|\bIntel\s+i[3579]\b", RegexOptions.IgnoreCase)]
    private static partial Regex CpuRegex();

    [GeneratedRegex(@"\b(?:AMD\s+)?Ryzen\s+([3579])\b", RegexOptions.IgnoreCase)]
    private static partial Regex RyzenCpuRegex();

    [GeneratedRegex(@"\b(8|16|32|64)\s*GB\b", RegexOptions.IgnoreCase)]
    private static partial Regex RamRegex();

    [GeneratedRegex(@"\b(?:(?:AMD\s+)?RADEON\s+|NVIDIA\s+|AMD\s+)?(?:RTX|GTX|RX)\s*-?\s*\d{3,4}(?:\s*(?:Ti|SUPER|XT|XTX))?\b", RegexOptions.IgnoreCase)]
    private static partial Regex GpuRegex();

    [GeneratedRegex(@"(\b(?:(?:AMD\s+)?RADEON\s+|NVIDIA\s+|AMD\s+)?(?:RTX|GTX|RX)\s*-?\s*\d{3,4}(?:\s*(?:Ti|SUPER|XT|XTX))?\b)\s*(?:[-/]\s*)?(?:8|16|32|64)\s*GB(?:\s+GDDR\w*)?", RegexOptions.IgnoreCase)]
    private static partial Regex GpuVramRegex();

    [GeneratedRegex(@"^(?:(?:AMD\s+)?RADEON|NVIDIA|AMD)\s+", RegexOptions.IgnoreCase)]
    private static partial Regex GpuVendorPrefixRegex();

    [GeneratedRegex(@"(?:Core\s+)?i([3579])", RegexOptions.IgnoreCase)]
    private static partial Regex CoreCpuRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}

public sealed record PriceRangeDefinition(string Value, string Label, decimal? MinPrice, decimal? MaxPrice);
public sealed record ProductParsedFacets(HashSet<string> Cpu, HashSet<string> Ram, HashSet<string> Gpu);
