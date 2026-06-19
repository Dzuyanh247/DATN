using System.Text.RegularExpressions;
using Datn.PcStore.Models;

namespace Datn.PcStore.Helpers;

public static partial class ProductFilterFacetHelper
{
    private const int MaxFacetLabelLength = 80;

    private static readonly HashSet<string> SupportedRamCapacities =
        new(["8", "16", "32", "64"], StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlyList<PriceRangeDefinition> PriceRanges =
    [
        new("under-1", "Dưới 1 triệu", null, 1_000_000m),
        new("1-2", "1 - 2 triệu", 1_000_000m, 2_000_000m),
        new("2-5", "2 - 5 triệu", 2_000_000m, 5_000_000m),
        new("5-10", "5 - 10 triệu", 5_000_000m, 10_000_000m),
        new("10-20", "10 - 20 triệu", 10_000_000m, 20_000_000m),
        new("20-30", "20 - 30 triệu", 20_000_000m, 30_000_000m),
        new("30-50", "30 - 50 triệu", 30_000_000m, 50_000_000m),
        new("over-50", "Trên 50 triệu", 50_000_000m, null)
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
        var storage = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var mainboard = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var psu = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var caseValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var cooling = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in sources)
        {
            cpu.UnionWith(ExtractMatches(source, CpuRegex(), match => NormalizeCpu(match.Value)));
            ram.UnionWith(ExtractRam(source));
            gpu.UnionWith(ExtractMatches(source, GpuRegex(), match => NormalizeGpu(match.Value)));
            storage.UnionWith(ExtractStorage(source));
            mainboard.UnionWith(ExtractSimpleComponent(source, MainboardRegex()));
            psu.UnionWith(ExtractSimpleComponent(source, PsuRegex()));
            caseValues.UnionWith(ExtractSimpleComponent(source, CaseRegex()));
            cooling.UnionWith(ExtractSimpleComponent(source, CoolingRegex()));
        }

        return new ProductParsedFacets(cpu, ram, gpu, storage, mainboard, psu, caseValues, cooling);
    }

    public static bool IsRenderableFilterOption(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return false;

        var trimmed = label.Trim();
        if (trimmed.Length > MaxFacetLabelLength)
            return false;

        return !trimmed.StartsWith('[')
            && !trimmed.StartsWith('{')
            && !trimmed.Contains("\"stt\"", StringComparison.OrdinalIgnoreCase)
            && !trimmed.Contains("\"description\"", StringComparison.OrdinalIgnoreCase)
            && !trimmed.Contains("\"quantity\"", StringComparison.OrdinalIgnoreCase)
            && !trimmed.Contains("\"warranty\"", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyCollection<string> GetFacetSources(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Specifications))
            return Array.Empty<string>();

        var sources = new List<string>();
        sources.AddRange(ProductComponentSpecHelper.ParseStored(product.Specifications)
            .Select(spec => spec.Description)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!));

        sources.AddRange(ProductSpecificationKeyValueHelper.ParseStored(product.Specifications)
            .Where(spec => !string.IsNullOrWhiteSpace(spec.Name) || !string.IsNullOrWhiteSpace(spec.Value))
            .Select(spec => $"{spec.Name}: {spec.Value}"));

        if (sources.Count == 0 && !LooksLikeRawJson(product.Specifications))
            sources.AddRange(product.Specifications.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries));

        return sources.Where(IsRenderableFilterOption).ToArray();
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


    private static HashSet<string> ExtractStorage(string source) =>
        StorageRegex().Matches(source)
            .Select(match => NormalizeStorage(match.Groups[1].Value))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static HashSet<string> ExtractSimpleComponent(string source, Regex regex) =>
        regex.IsMatch(source) ? new HashSet<string>([regex.Match(source).Value.Trim()], StringComparer.OrdinalIgnoreCase) : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private static string NormalizeStorage(string value)
    {
        var normalized = value.Trim().ToUpperInvariant().Replace(" ", string.Empty);
        return normalized switch
        {
            "256GB" => "256GB",
            "512GB" => "512GB",
            "1TB" or "1024GB" => "1TB",
            "2TB" or "2048GB" => "2TB",
            "4TB" or "4096GB" => "4TB",
            _ => string.Empty
        };
    }

    private static bool LooksLikeRawJson(string? text)
    {
        var trimmed = text?.TrimStart();
        return trimmed?.StartsWith('[') == true || trimmed?.StartsWith('{') == true;
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
            return $"AMD Ryzen {ryzenMatch.Groups[1].Value}";

        var coreMatch = CoreCpuRegex().Match(normalized);
        if (coreMatch.Success)
            return $"Intel Core i{coreMatch.Groups[1].Value}";

        var ultraMatch = CoreUltraCpuRegex().Match(normalized);
        return ultraMatch.Success ? $"Intel Core Ultra {ultraMatch.Groups[1].Value}" : normalized;
    }

    private static string NormalizeGpu(string value)
    {
        var normalized = WhitespaceRegex().Replace(value.Replace('-', ' ').Trim(), " ").ToUpperInvariant();
        normalized = GpuVendorPrefixRegex().Replace(normalized, string.Empty);
        if (Regex.IsMatch(normalized, @"\bRTX\s*3050\b", RegexOptions.IgnoreCase)) return "NVIDIA RTX 3050";
        if (Regex.IsMatch(normalized, @"\bRTX\s*4060\s*TI\b", RegexOptions.IgnoreCase)) return "NVIDIA RTX 4060 Ti";
        if (Regex.IsMatch(normalized, @"\bRTX\s*4060\b", RegexOptions.IgnoreCase)) return "NVIDIA RTX 4060";
        if (Regex.IsMatch(normalized, @"\bRTX\s*4070\s*TI\b", RegexOptions.IgnoreCase)) return "NVIDIA RTX 4070 Ti";
        if (Regex.IsMatch(normalized, @"\bRTX\s*4070\b", RegexOptions.IgnoreCase)) return "NVIDIA RTX 4070";
        if (Regex.IsMatch(normalized, @"\bRTX\s*4080\b", RegexOptions.IgnoreCase)) return "NVIDIA RTX 4080";
        if (Regex.IsMatch(normalized, @"\bRTX\s*4090\b", RegexOptions.IgnoreCase)) return "NVIDIA RTX 4090";
        if (Regex.IsMatch(normalized, @"\bRX\s*7600\b", RegexOptions.IgnoreCase)) return "AMD RX 7600";
        if (Regex.IsMatch(normalized, @"\bRX\s*7700\s*XT\b", RegexOptions.IgnoreCase)) return "AMD RX 7700 XT";
        if (Regex.IsMatch(normalized, @"\bRX\s*7800\s*XT\b", RegexOptions.IgnoreCase)) return "AMD RX 7800 XT";
        if (Regex.IsMatch(normalized, @"\bRX\s*7900\s*(XT|XTX)\b", RegexOptions.IgnoreCase)) return "AMD RX 7900 XT/XTX";
        return string.Empty;
    }

    [GeneratedRegex(@"\b(?:AMD\s+)?Ryzen\s+[3579](?:\s+PRO)?\b|\b(?:Intel\s+)?Core\s+i[3579]\b|\bIntel\s+i[3579]\b|\bIntel\s+Core\s+Ultra\s+[579]\b", RegexOptions.IgnoreCase)]
    private static partial Regex CpuRegex();

    [GeneratedRegex(@"\b(?:AMD\s+)?Ryzen\s+([3579])\b", RegexOptions.IgnoreCase)]
    private static partial Regex RyzenCpuRegex();

    [GeneratedRegex(@"\b(8|16|32|64)\s*GB\b", RegexOptions.IgnoreCase)]
    private static partial Regex RamRegex();

    [GeneratedRegex(@"\b(?:(?:AMD\s+)?RADEON\s+|NVIDIA\s+|AMD\s+)?(?:RTX|GTX|RX)\s*-?\s*(?:3050|4060|4070|4080|4090|7600|7700|7800|7900)(?:\s*(?:Ti|XT|XTX))?\b", RegexOptions.IgnoreCase)]
    private static partial Regex GpuRegex();

    [GeneratedRegex(@"(\b(?:(?:AMD\s+)?RADEON\s+|NVIDIA\s+|AMD\s+)?(?:RTX|GTX|RX)\s*-?\s*\d{3,4}(?:\s*(?:Ti|SUPER|XT|XTX))?\b)\s*(?:[-/]\s*)?(?:8|16|32|64)\s*GB(?:\s+GDDR\w*)?", RegexOptions.IgnoreCase)]
    private static partial Regex GpuVramRegex();

    [GeneratedRegex(@"^(?:(?:AMD\s+)?RADEON|NVIDIA|AMD)\s+", RegexOptions.IgnoreCase)]
    private static partial Regex GpuVendorPrefixRegex();

    [GeneratedRegex(@"(?:Core\s+)?i([3579])", RegexOptions.IgnoreCase)]
    private static partial Regex CoreCpuRegex();

    [GeneratedRegex(@"Core\s+Ultra\s+([579])", RegexOptions.IgnoreCase)]
    private static partial Regex CoreUltraCpuRegex();

    [GeneratedRegex(@"\b(256\s*GB|512\s*GB|1\s*TB|1024\s*GB|2\s*TB|2048\s*GB|4\s*TB|4096\s*GB)\b", RegexOptions.IgnoreCase)]
    private static partial Regex StorageRegex();

    [GeneratedRegex(@"\b(?:Mainboard|Bo mạch chủ|Motherboard)[^\r\n]{0,60}", RegexOptions.IgnoreCase)]
    private static partial Regex MainboardRegex();

    [GeneratedRegex(@"\b(?:PSU|Nguồn|Power Supply)[^\r\n]{0,60}", RegexOptions.IgnoreCase)]
    private static partial Regex PsuRegex();

    [GeneratedRegex(@"\b(?:Case|Vỏ case|Vo case)[^\r\n]{0,60}", RegexOptions.IgnoreCase)]
    private static partial Regex CaseRegex();

    [GeneratedRegex(@"\b(?:Cooling|Tản nhiệt|Tan nhiet|Cooler)[^\r\n]{0,60}", RegexOptions.IgnoreCase)]
    private static partial Regex CoolingRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}

public sealed record PriceRangeDefinition(string Value, string Label, decimal? MinPrice, decimal? MaxPrice);
public sealed record ProductParsedFacets(HashSet<string> Cpu, HashSet<string> Ram, HashSet<string> Gpu, HashSet<string> Storage, HashSet<string> Mainboard, HashSet<string> Psu, HashSet<string> Case, HashSet<string> Cooling);
