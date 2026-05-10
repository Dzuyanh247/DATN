using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Datn.PcStore.Services;

public static class AddressQueryHelper
{
    public static string NormalizeSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var cleaned = Regex.Replace(value.Trim(), "\\s+", " ");
        cleaned = Regex.Replace(cleaned, "\\s*,\\s*", ", ");
        cleaned = Regex.Replace(cleaned, ",+", ",");
        return cleaned.Trim(' ', ',');
    }

    public static string BuildNormalizedAddress(string? addressDetail, string? wardName, string? provinceName, string? country = "Vietnam")
    {
        var segments = new List<string>();
        var detail = NormalizeSegment(addressDetail);
        var ward = NormalizeSegment(wardName);
        var province = NormalizeSegment(provinceName);
        var countrySegment = NormalizeSegment(country);

        if (!string.IsNullOrWhiteSpace(detail)) segments.Add(detail);
        if (!ContainsLoose(detail, ward) && !string.IsNullOrWhiteSpace(ward)) segments.Add(ward);
        if (!ContainsLoose(detail, province) && !ContainsLoose(ward, province) && !string.IsNullOrWhiteSpace(province)) segments.Add(province);
        if (!ContainsLoose(string.Join(", ", segments), countrySegment) && !string.IsNullOrWhiteSpace(countrySegment)) segments.Add(countrySegment);

        return string.Join(", ", DeduplicateSegments(segments));
    }

    public static bool IsTooShortOrNumericOnly(string? detail)
    {
        var normalized = NormalizeSegment(detail);
        return normalized.Length < 3 || Regex.IsMatch(normalized, "^\\d+$");
    }

    public static string Fold(string? value)
    {
        var text = NormalizeSegment(value).ToLowerInvariant();
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D');
    }

    private static bool ContainsLoose(string? haystack, string? needle)
    {
        var h = Fold(haystack);
        var n = Fold(needle);
        return !string.IsNullOrWhiteSpace(n) && h.Contains(n, StringComparison.Ordinal);
    }

    private static IEnumerable<string> DeduplicateSegments(IEnumerable<string> segments)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var segment in segments.Select(NormalizeSegment).Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var key = Fold(segment);
            if (seen.Add(key)) yield return segment;
        }
    }
}
