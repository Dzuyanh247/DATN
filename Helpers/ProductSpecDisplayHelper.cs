using System.Text.RegularExpressions;
using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;

namespace Datn.PcStore.Helpers;

public static class ProductSpecDisplayHelper
{
    private const int DefaultSpecMaxLength = 92;

    private static readonly string[] SpecGroupOrder = { "CPU", "Mainboard", "RAM", "SSD", "GPU", "PSU" };

    private static readonly Dictionary<string, string[]> GroupKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CPU"] = new[] { "cpu", "processor", "core i", "ryzen", "xeon", " i3", " i5", " i7", " i9" },
        ["Mainboard"] = new[] { "main", "mainboard", "motherboard", "bo mạch", "b760", "b650", "x670", "z790", "h610", "a620" },
        ["RAM"] = new[] { "ram", "memory", "ddr3", "ddr4", "ddr5", "dimm" },
        ["SSD"] = new[] { "ssd", "nvme", "m.2", "sata", "ổ cứng", "o cung", "hdd", "storage" },
        ["GPU"] = new[] { "gpu", "vga", "card màn", "card man", "geforce", "rtx", "gtx", "radeon", "rx " },
        ["PSU"] = new[] { "psu", "nguồn", "nguon", "power", "watt", "850w", "750w", "650w", "550w" }
    };

    public static List<ProductComponentSpecViewModel> TryParseComponentSpecs(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || LooksLikeJsonButNotComponentSpecs(raw))
        {
            return new List<ProductComponentSpecViewModel>();
        }

        return ProductComponentSpecHelper.ParseStored(raw);
    }

    public static List<string> GetHoverSpecs(Product product, int maxItems = 6)
    {
        var specCandidates = TryParseComponentSpecs(product.TechnicalSpecifications)
            .Select(x => x.Description)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (!specCandidates.Any())
        {
            specCandidates = GetSafeFallbackLines(product.ShortDescription, product.Description, product.DetailDescription)
                .ToList();
        }

        return specCandidates
            .Where(x => !ContainsRawJsonMarkers(x))
            .Select(x => new { Group = GetSpecGroup(x), Text = Truncate(RemoveLegacySpecMetadata(x), DefaultSpecMaxLength) })
            .Where(x => !string.IsNullOrWhiteSpace(x.Text))
            .GroupBy(x => x.Group == "Khác" ? x.Text : x.Group, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(x => GetGroupPriority(x.Group))
            .ThenBy(x => x.Text)
            .Take(Math.Max(1, maxItems))
            .Select(x => x.Text)
            .ToList();
    }

    public static string GetSpecGroup(string? description)
    {
        var text = NormalizeSearchText(description);
        if (string.IsNullOrWhiteSpace(text)) return "Khác";

        foreach (var group in SpecGroupOrder)
        {
            if (GroupKeywords[group].Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                return group;
            }
        }

        return "Khác";
    }

    public static string Truncate(string? text, int maxLength)
    {
        var cleaned = CleanLine(text);
        if (maxLength <= 0 || cleaned.Length <= maxLength) return cleaned;
        return cleaned[..Math.Max(0, maxLength - 1)].TrimEnd() + "…";
    }

    public static List<string> GetSafeTextLines(Product product, int maxItems = 5, int maxLength = 180)
    {
        return GetSafeFallbackLines(product.ShortDescription, product.Description, product.DetailDescription)
            .Select(x => Truncate(x, maxLength))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Take(Math.Max(1, maxItems))
            .ToList();
    }

    public static string GetSummaryText(Product product, int maxLength = 180)
    {
        return GetSafeTextLines(product, 1, maxLength).FirstOrDefault() ?? string.Empty;
    }

    public static string GetPromotionText(Product product, int maxLength = 220)
    {
        var productSpecs = CleanLine(product.TechnicalSpecifications);
        var candidates = product.IsPromotion
            ? new[] { product.DetailDescription, product.Description, product.ShortDescription }
            : new[] { product.DetailDescription };

        return candidates
            .Select(CleanLine)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Where(x => !ContainsRawJsonMarkers(x))
            .Where(x => !string.Equals(x, productSpecs, StringComparison.OrdinalIgnoreCase))
            .Select(x => Truncate(x, maxLength))
            .FirstOrDefault() ?? string.Empty;
    }

    private static IEnumerable<string> GetSafeFallbackLines(params string?[] values)
    {
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value) || ContainsRawJsonMarkers(value)) continue;

            foreach (var line in Regex.Split(value, @"\r?\n"))
            {
                var cleaned = RemoveLegacySpecMetadata(line);
                if (string.IsNullOrWhiteSpace(cleaned) || IsHeaderLine(cleaned) || ContainsRawJsonMarkers(cleaned)) continue;
                yield return cleaned;
            }
        }
    }

    private static int GetGroupPriority(string group)
    {
        var index = Array.IndexOf(SpecGroupOrder, group);
        return index >= 0 ? index : SpecGroupOrder.Length;
    }

    private static string RemoveLegacySpecMetadata(string? text)
    {
        var cleaned = CleanLine(text).Trim(' ', '-', '•', '*', '+');
        cleaned = Regex.Replace(cleaned, @"^\d+\s*[\.|\)|\-]?\s*", string.Empty).Trim();
        cleaned = Regex.Replace(cleaned, @"\s+\d+\s+([0-9]+\s*tháng|[0-9]+th|[0-9]+t)$", string.Empty, RegexOptions.IgnoreCase).Trim();
        return cleaned;
    }

    private static bool LooksLikeJsonButNotComponentSpecs(string raw)
    {
        var trimmed = raw.TrimStart();
        return (trimmed.StartsWith('{') || trimmed.StartsWith('['))
            && ProductComponentSpecHelper.TryDeserialize(raw).Count == 0;
    }

    private static bool ContainsRawJsonMarkers(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var trimmed = text.TrimStart();
        return trimmed.StartsWith('[')
            || trimmed.StartsWith('{')
            || text.Contains("\"description\"", StringComparison.OrdinalIgnoreCase)
            || text.Contains("\"stt\"", StringComparison.OrdinalIgnoreCase)
            || text.Contains("\"quantity\"", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHeaderLine(string line)
    {
        var normalized = NormalizeSearchText(line);
        return normalized.StartsWith("stt ")
            || normalized.Contains("mo ta thiet bi sl bh")
            || normalized.Contains("mô tả thiết bị sl bh")
            || normalized is "thong so ky thuat" or "thông số kỹ thuật";
    }

    private static string NormalizeSearchText(string? value)
        => Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim().ToLowerInvariant();

    private static string CleanLine(string? value)
        => Regex.Replace(value ?? string.Empty, @"[ \t\r\n]+", " ").Trim();
}
