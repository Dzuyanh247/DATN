using System.Text.RegularExpressions;
using Datn.PcStore.Models;

namespace Datn.PcStore.Helpers;

public static partial class ProductFilterFacetHelper
{
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
        var source = string.Join(' ', new[]
        {
            product.Name,
            product.ShortDescription,
            product.Description,
            product.DetailDescription,
            product.Specifications
        }.Where(value => !string.IsNullOrWhiteSpace(value)));

        return new ProductParsedFacets(
            ExtractMatches(source, CpuRegex(), match => NormalizeCpu(match.Value)),
            ExtractMatches(source, RamRegex(), match => $"{match.Groups[1].Value}GB"),
            ExtractMatches(source, GpuRegex(), match => NormalizeGpu(match.Value)));
    }

    private static HashSet<string> ExtractMatches(string source, Regex regex, Func<Match, string> selector) =>
        regex.Matches(source)
            .Select(selector)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static string NormalizeCpu(string value)
    {
        var normalized = WhitespaceRegex().Replace(value.Trim(), " ");
        if (normalized.StartsWith("Ryzen", StringComparison.OrdinalIgnoreCase))
            return $"Ryzen {normalized[^1]}";

        var coreMatch = CoreCpuRegex().Match(normalized);
        return coreMatch.Success ? $"Core i{coreMatch.Groups[1].Value}" : normalized;
    }

    private static string NormalizeGpu(string value)
    {
        var normalized = WhitespaceRegex().Replace(value.Replace('-', ' ').Trim(), " ").ToUpperInvariant();
        return normalized.Replace(" TI", " Ti", StringComparison.Ordinal).Replace(" SUPER", " SUPER", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"\bRyzen\s+[3579]\b|\b(?:Intel\s+)?Core\s+i[3579]\b|\bIntel\s+i[3579]\b", RegexOptions.IgnoreCase)]
    private static partial Regex CpuRegex();

    [GeneratedRegex(@"(?:\bRAM\s*[:\-]?\s*|\b)(\d{1,3})\s*GB\s*(?:DDR[345])\b", RegexOptions.IgnoreCase)]
    private static partial Regex RamRegex();

    [GeneratedRegex(@"\b(?:RTX|GTX|RX)\s*-?\s*\d{3,4}(?:\s*(?:Ti|SUPER|XT|XTX))?\b", RegexOptions.IgnoreCase)]
    private static partial Regex GpuRegex();

    [GeneratedRegex(@"i([3579])", RegexOptions.IgnoreCase)]
    private static partial Regex CoreCpuRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}

public sealed record PriceRangeDefinition(string Value, string Label, decimal? MinPrice, decimal? MaxPrice);
public sealed record ProductParsedFacets(HashSet<string> Cpu, HashSet<string> Ram, HashSet<string> Gpu);
