using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace Datn.PcStore.Services;

public class OpenRouteServiceProvider : IMapProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<OpenRouteServiceProvider> _logger;

    public OpenRouteServiceProvider(HttpClient httpClient, IConfiguration configuration, ILogger<OpenRouteServiceProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["OPENROUTESERVICE_API_KEY"]
            ?? configuration["OpenRouteService:ApiKey"]
            ?? throw new InvalidOperationException("Thiếu cấu hình OPENROUTESERVICE_API_KEY.");
    }

    public string ProviderName => "OpenRouteService";

    public async Task<AddressSearchResult> SearchAddressesAsync(string query, string? provinceName = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return new AddressSearchResult(string.Empty, Array.Empty<AddressSuggestion>());

        var normalizedQuery = NormalizeQuery(query);
        var cleanedNoComma = NormalizeQuery(normalizedQuery.Replace(",", " "));
        var fallbackQueries = BuildFallbackQueries(normalizedQuery, cleanedNoComma, provinceName);

        foreach (var candidate in fallbackQueries)
        {
            var suggestions = await SearchInternalAsync(candidate, provinceName, null, cancellationToken);
            if (suggestions.Count > 0) return new AddressSearchResult(candidate, suggestions);
        }

        return new AddressSearchResult(fallbackQueries.LastOrDefault() ?? normalizedQuery, Array.Empty<AddressSuggestion>());
    }

    private async Task<IReadOnlyList<AddressSuggestion>> SearchInternalAsync(string query, string? provinceName, string? wardName, CancellationToken cancellationToken)
    {
        var requestUri = BuildGeocodeUri("https://api.openrouteservice.org/geocode/search", new Dictionary<string, string?>
        {
            ["api_key"] = _apiKey,
            ["text"] = query,
            ["size"] = "8",
            ["lang"] = "vi",
            ["boundary_country"] = "VN"
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.TryAddWithoutValidation("Authorization", _apiKey);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return Array.Empty<AddressSuggestion>();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!json.RootElement.TryGetProperty("features", out var features)) return Array.Empty<AddressSuggestion>();

        var ranked = new List<(AddressSuggestion Suggestion, int Score)>();
        foreach (var feature in features.EnumerateArray())
        {
            if (!TryExtractFeature(feature, out var label, out var lat, out var lon, out var region, out var county, out var locality, out var localadmin, out var reason))
            {
                _logger.LogInformation("ORS feature rejected reason={Reason}", reason);
                continue;
            }

            _logger.LogInformation("ORS feature label='{Label}' lat={Lat} lng={Lng} region='{Region}' county='{County}' locality='{Locality}'", label, lat, lon, region, county, locality);
            if (!IsFeatureInProvince(label, region, county, locality, localadmin, provinceName) || ContainsExcludedCity(label, region, county, locality, localadmin, provinceName))
            {
                _logger.LogInformation("ORS feature rejected label='{Label}' reason=province_mismatch_or_excluded", label);
                continue;
            }

            var score = ScoreFeature(query, label, wardName, provinceName, region, county, locality, localadmin);
            ranked.Add((new AddressSuggestion(label, lat, lon, label), score));
        }

        return ranked.OrderByDescending(x => x.Score).Select(x => x.Suggestion).Take(6).ToList();
    }

    public async Task<GeoPoint?> GeocodeAsync(string address, string? provinceName = null, string? wardName = null, CancellationToken cancellationToken = default)
    {
        var normalizedAddress = NormalizeAddressText(address);
        var requestUri = BuildGeocodeUri("https://api.openrouteservice.org/geocode/search", new Dictionary<string, string?>
        {
            ["api_key"] = _apiKey,
            ["text"] = normalizedAddress,
            ["size"] = "8",
            ["boundary_country"] = "VN",
            ["lang"] = "vi"
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.TryAddWithoutValidation("Authorization", _apiKey);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!json.RootElement.TryGetProperty("features", out var features) || features.GetArrayLength() == 0) return null;

        var ranked = new List<(GeoPoint Point, int Score, string Label)>();
        foreach (var feature in features.EnumerateArray())
        {
            if (!TryExtractFeature(feature, out var label, out var lat, out var lon, out var region, out var county, out var locality, out var localadmin, out var reason))
            {
                _logger.LogInformation("ORS geocode feature rejected reason={Reason}", reason);
                continue;
            }

            _logger.LogInformation("ORS geocode feature label='{Label}' lat={Lat} lng={Lng} region='{Region}' county='{County}' locality='{Locality}'", label, lat, lon, region, county, locality);
            if (!IsFeatureInProvince(label, region, county, locality, localadmin, provinceName) || ContainsExcludedCity(label, region, county, locality, localadmin, provinceName))
            {
                _logger.LogInformation("ORS geocode rejected label='{Label}' reason=province_mismatch_or_excluded", label);
                continue;
            }

            ranked.Add((new GeoPoint(lat, lon), ScoreFeature(normalizedAddress, label, wardName, provinceName, region, county, locality, localadmin), label));
        }

        if (ranked.Count == 0) return null;
        var selected = ranked.OrderByDescending(x => x.Score).First();
        _logger.LogInformation("ORS geocode selected feature label='{Label}' lat={Lat} lng={Lng} score={Score}", selected.Label, selected.Point.Latitude, selected.Point.Longitude, selected.Score);
        return selected.Point;
    }

    public async Task<RouteMetrics?> GetRouteMetricsAsync(GeoPoint origin, GeoPoint destination, CancellationToken cancellationToken = default)
    {
        var body = new { coordinates = new[] { new[] { origin.Longitude, origin.Latitude }, new[] { destination.Longitude, destination.Latitude } } };
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openrouteservice.org/v2/directions/driving-car");
        request.Headers.TryAddWithoutValidation("Authorization", _apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var summary = json.RootElement.GetProperty("routes")[0].GetProperty("summary");
        return new RouteMetrics(Math.Round(summary.GetProperty("distance").GetDecimal() / 1000m, 2), (int)Math.Ceiling(summary.GetProperty("duration").GetDouble() / 60d));
    }

    private static bool TryExtractFeature(JsonElement feature, out string label, out double lat, out double lon, out string region, out string county, out string locality, out string localadmin, out string reason)
    {
        label = region = county = locality = localadmin = string.Empty;
        lat = lon = 0;
        reason = "";
        if (!feature.TryGetProperty("properties", out var props)) { reason = "missing_properties"; return false; }
        label = GetPropertyString(props, "label");
        region = GetPropertyString(props, "region"); county = GetPropertyString(props, "county"); locality = GetPropertyString(props, "locality"); localadmin = GetPropertyString(props, "localadmin");
        if (string.IsNullOrWhiteSpace(label)) { reason = "missing_label"; return false; }
        if (!feature.TryGetProperty("geometry", out var geo) || !geo.TryGetProperty("coordinates", out var coor) || coor.GetArrayLength() < 2) { reason = "missing_coordinates"; return false; }
        lon = coor[0].GetDouble(); lat = coor[1].GetDouble();
        if (!IsValidCoordinate(lat, lon)) { reason = "invalid_coordinates"; return false; }
        return true;
    }

    private static bool IsValidCoordinate(double lat, double lon) => lat is >= -90 and <= 90 && lon is >= -180 and <= 180;
    private static string GetPropertyString(JsonElement props, string name) => props.TryGetProperty(name, out var x) ? x.GetString() ?? string.Empty : string.Empty;
    private static bool IsFeatureInProvince(string label, string region, string county, string locality, string localadmin, string? provinceName)
    {
        if (string.IsNullOrWhiteSpace(provinceName)) return true;
        var province = RemoveDiacritics(provinceName).ToLowerInvariant();
        var haystack = RemoveDiacritics($"{label} {region} {county} {locality} {localadmin}").ToLowerInvariant();
        if (province.Contains("ha noi")) return haystack.Contains("ha noi") || haystack.Contains("hanoi");
        return haystack.Contains(province);
    }
    private static bool ContainsExcludedCity(string label, string region, string county, string locality, string localadmin, string? provinceName)
    {
        if (string.IsNullOrWhiteSpace(provinceName)) return false;
        var province = RemoveDiacritics(provinceName).ToLowerInvariant();
        if (!province.Contains("ha noi")) return false;
        var haystack = RemoveDiacritics($"{label} {region} {county} {locality} {localadmin}").ToLowerInvariant();
        return haystack.Contains("ho chi minh") || haystack.Contains("thanh pho ho chi minh") || haystack.Contains("tp hcm") || haystack.Contains("tp. ho chi minh");
    }
    private static int ScoreFeature(string query, string label, string? ward, string? province, string region, string county, string locality, string localadmin)
    {
        var score = 0; var q = RemoveDiacritics(query).ToLowerInvariant(); var l = RemoveDiacritics(label).ToLowerInvariant();
        var primary = q.Split(',')[0].Trim();
        if (!string.IsNullOrWhiteSpace(primary) && l.Contains(primary)) score += 60;
        if (Regex.IsMatch(primary, "^\\d+")) score += 20;
        if (!string.IsNullOrWhiteSpace(ward) && l.Contains(RemoveDiacritics(ward).ToLowerInvariant())) score += 30;
        if (!string.IsNullOrWhiteSpace(province) && IsFeatureInProvince(label, region, county, locality, localadmin, province)) score += 50;
        return score;
    }

    private static IReadOnlyList<string> BuildFallbackQueries(string normalizedQuery, string cleanedNoComma, string? provinceName)
    {
        var province = NormalizeAddressText(provinceName ?? string.Empty);
        var noDoubleComma = NormalizeAddressText(normalizedQuery.Replace(",,", ","));
        return new[]
        {
            ComposeAddress(noDoubleComma, province, "Vietnam"),
            ComposeAddress(cleanedNoComma, province, "Vietnam"),
            ComposeAddress(noDoubleComma, "Vietnam"),
            noDoubleComma
        }.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string NormalizeQuery(string query) => Regex.Replace(query.Trim(), "\\s+", " ");
    private static string NormalizeAddressText(string query) => NormalizeQuery(Regex.Replace((query ?? string.Empty).Replace(",,", ","), "\\s*,\\s*", ", ")).Trim(',',' ');
    private static string ComposeAddress(params string[] parts) => string.Join(", ", parts.Where(x => !string.IsNullOrWhiteSpace(x)).Select(NormalizeAddressText).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase));
    private static Uri BuildGeocodeUri(string endpoint, IReadOnlyDictionary<string, string?> query) => new(QueryHelpers.AddQueryString(endpoint, query.Where(kv => !string.IsNullOrWhiteSpace(kv.Value)).ToDictionary(kv => kv.Key, kv => kv.Value!)));
    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalizedString) if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D');
    }
}
