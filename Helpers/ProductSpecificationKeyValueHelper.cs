using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Datn.PcStore.ViewModels;

namespace Datn.PcStore.Helpers;

public static class ProductSpecificationKeyValueHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static string Serialize(IEnumerable<ProductSpecificationItemVm>? items)
    {
        var normalized = Normalize(items).ToList();
        return normalized.Any() ? JsonSerializer.Serialize(normalized, JsonOptions) : string.Empty;
    }

    public static List<ProductSpecificationItemVm> ParseStored(string? storedText)
    {
        if (string.IsNullOrWhiteSpace(storedText)) return new List<ProductSpecificationItemVm>();
        var text = storedText.Trim();
        if (text.StartsWith('['))
        {
            try
            {
                var items = JsonSerializer.Deserialize<List<ProductSpecificationItemVm>>(text, JsonOptions);
                var normalized = Normalize(items).ToList();
                if (normalized.Any()) return normalized;
            }
            catch (JsonException)
            {
                // Fall through to legacy text parsing.
            }
        }

        return ParseFallbackText(text);
    }

    public static List<ProductSpecificationItemVm> ParseFallbackText(string? rawText)
    {
        var result = new List<ProductSpecificationItemVm>();
        if (string.IsNullOrWhiteSpace(rawText)) return result;

        foreach (var rawLine in Regex.Split(rawText, @"\r?\n|\s+\|\s+"))
        {
            var line = Clean(rawLine).Trim('-', '•', '*', '+', ' ');
            if (string.IsNullOrWhiteSpace(line) || IsHeader(line)) continue;

            var separatorIndex = line.IndexOf(':');
            if (separatorIndex < 0) separatorIndex = line.IndexOf('：');

            if (separatorIndex > 0)
            {
                result.Add(new ProductSpecificationItemVm
                {
                    Name = Clean(line[..separatorIndex]),
                    Value = Clean(line[(separatorIndex + 1)..])
                });
            }
            else
            {
                result.Add(new ProductSpecificationItemVm
                {
                    Name = "Thông số",
                    Value = Clean(line)
                });
            }
        }

        return Normalize(result).ToList();
    }

    private static IEnumerable<ProductSpecificationItemVm> Normalize(IEnumerable<ProductSpecificationItemVm>? items)
    {
        if (items == null) yield break;
        var emitted = new List<ProductSpecificationItemVm>();
        foreach (var item in items)
        {
            var name = Clean(item.Name);
            var value = Clean(item.Value);
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value)) continue;
            var key = BuildKey(name);
            var existing = emitted.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Value = MergeValues(existing.Value, value);
                continue;
            }

            var pair = new ProductSpecificationItemVm { Name = name, Value = value, Key = key };
            emitted.Add(pair);
            yield return pair;
        }
    }

    private static bool IsHeader(string value)
    {
        var normalized = Clean(value).ToLowerInvariant();
        return normalized is "chi tiết" or "chi tiet" or "details" or "general" or "thông số" or "thong so" or "thông số kỹ thuật" or "thong so ky thuat" or "tên thông số giá trị" or "ten thong so gia tri";
    }

    public static string BuildKey(string? name)
    {
        var normalized = (name ?? string.Empty).Trim().ToLowerInvariant().Replace("đ", "d");
        var decomposed = normalized.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        var previousDash = false;
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
                previousDash = false;
            }
            else if (!previousDash)
            {
                builder.Append('-');
                previousDash = true;
            }
        }
        return builder.ToString().Trim('-');
    }

    private static string MergeValues(string? current, string next)
    {
        var values = Regex.Split(current ?? string.Empty, @"\s*;\s*").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        if (!values.Contains(next, StringComparer.OrdinalIgnoreCase)) values.Add(next);
        return string.Join("; ", values);
    }

    private static string Clean(string? value) => Regex.Replace(value ?? string.Empty, @"[ \t\r\n]+", " ").Trim();
}
