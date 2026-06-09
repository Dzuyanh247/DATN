using System.Text.Json;
using System.Text.RegularExpressions;
using Datn.PcStore.ViewModels;

namespace Datn.PcStore.Helpers;

public static class ProductComponentSpecHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Regex WhitespaceSpecRegex = new(
        @"^\s*(\d+)\s+(.+?)\s+(\d+)\s+([0-9]+th|[0-9]+\s*tháng|[0-9]+)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string Serialize(List<ProductComponentSpecViewModel>? specs)
    {
        var normalized = Normalize(specs);
        return normalized.Any() ? JsonSerializer.Serialize(normalized, JsonOptions) : string.Empty;
    }

    public static List<ProductComponentSpecViewModel> ParseStored(string? storedText)
    {
        if (string.IsNullOrWhiteSpace(storedText)) return new List<ProductComponentSpecViewModel>();

        var jsonSpecs = TryDeserialize(storedText);
        return jsonSpecs.Count > 0 ? jsonSpecs : ParseFallbackText(storedText);
    }

    public static List<ProductComponentSpecViewModel> TryDeserialize(string? storedText)
    {
        if (string.IsNullOrWhiteSpace(storedText)) return new List<ProductComponentSpecViewModel>();

        var text = storedText.Trim();
        if (!text.StartsWith('[')) return new List<ProductComponentSpecViewModel>();

        try
        {
            return Normalize(JsonSerializer.Deserialize<List<ProductComponentSpecViewModel>>(text, JsonOptions));
        }
        catch (JsonException)
        {
            return new List<ProductComponentSpecViewModel>();
        }
    }

    public static List<ProductComponentSpecViewModel> ParseFallbackText(string? rawText)
    {
        var specs = new List<ProductComponentSpecViewModel>();
        if (string.IsNullOrWhiteSpace(rawText)) return specs;

        foreach (var rawLine in Regex.Split(rawText, @"\r?\n"))
        {
            var line = CleanLine(rawLine);
            if (string.IsNullOrWhiteSpace(line) || IsHeaderLine(line)) continue;

            var columns = SplitComponentColumns(rawLine);
            if (columns.Length >= 4 && IsHeaderLine(string.Join(' ', columns))) continue;

            ProductComponentSpecViewModel? spec = null;
            if (columns.Length >= 4)
            {
                spec = new ProductComponentSpecViewModel
                {
                    Stt = ParsePositiveInt(columns[0], specs.Count + 1),
                    Description = columns[1],
                    Quantity = ParsePositiveInt(columns[2], 1),
                    Warranty = columns[3]
                };
            }
            else
            {
                var match = WhitespaceSpecRegex.Match(line);
                if (match.Success)
                {
                    spec = new ProductComponentSpecViewModel
                    {
                        Stt = ParsePositiveInt(match.Groups[1].Value, specs.Count + 1),
                        Description = CleanLine(match.Groups[2].Value),
                        Quantity = ParsePositiveInt(match.Groups[3].Value, 1),
                        Warranty = CleanLine(match.Groups[4].Value)
                    };
                }
                else
                {
                    var description = Regex.Replace(line, @"^\s*\d+\s+", string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        spec = new ProductComponentSpecViewModel
                        {
                            Stt = specs.Count + 1,
                            Description = description,
                            Quantity = 1,
                            Warranty = string.Empty
                        };
                    }
                }
            }

            if (spec != null && !string.IsNullOrWhiteSpace(spec.Description))
            {
                specs.Add(spec);
            }
        }

        return Normalize(specs);
    }

    public static bool IsComponentSpecJson(string? storedText) => TryDeserialize(storedText).Count > 0;

    private static List<ProductComponentSpecViewModel> Normalize(List<ProductComponentSpecViewModel>? specs)
    {
        if (specs == null) return new List<ProductComponentSpecViewModel>();

        var normalized = new List<ProductComponentSpecViewModel>();
        foreach (var spec in specs)
        {
            var description = CleanLine(spec.Description);
            if (string.IsNullOrWhiteSpace(description) || IsHeaderLine(description)) continue;

            normalized.Add(new ProductComponentSpecViewModel
            {
                Stt = spec.Stt.GetValueOrDefault() > 0 ? spec.Stt : normalized.Count + 1,
                Description = description,
                Quantity = spec.Quantity.GetValueOrDefault() > 0 ? spec.Quantity : 1,
                Warranty = CleanLine(spec.Warranty)
            });
        }

        return normalized;
    }

    private static bool IsHeaderLine(string line)
    {
        var normalized = Regex.Replace(line, @"\s+", " ").Trim().ToLowerInvariant();
        return normalized == "stt mô tả thiết bị sl bh"
            || normalized == "stt mo ta thiet bi sl bh"
            || (normalized.StartsWith("stt ") && normalized.Contains("mô tả") && normalized.Contains(" sl") && normalized.EndsWith("bh"));
    }

    private static string[] SplitComponentColumns(string rawLine)
    {
        var separator = rawLine.Contains('\t') ? '\t' : rawLine.Contains('|') ? '|' : '\0';
        if (separator == '\0') return Array.Empty<string>();

        return rawLine.Split(separator)
            .Select(CleanLine)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();
    }

    private static string CleanLine(string? value)
        => Regex.Replace(value ?? string.Empty, @"[ \t\r\n]+", " ").Trim();

    private static int ParsePositiveInt(string? value, int fallback)
        => int.TryParse((value ?? string.Empty).Trim(), out var result) && result > 0 ? result : fallback;
}
