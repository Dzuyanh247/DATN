using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace Datn.PcStore.Services;

public class OpenRouteServiceProvider : IMapProvider
{
    private const int ScoreThreshold = 150;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<OpenRouteServiceProvider> _logger;

    public OpenRouteServiceProvider(HttpClient httpClient, IConfiguration configuration, ILogger<OpenRouteServiceProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["OPENROUTESERVICE_API_KEY"] ?? configuration["OpenRouteService:ApiKey"] ?? throw new InvalidOperationException("Thiếu cấu hình OPENROUTESERVICE_API_KEY.");
    }

    public string ProviderName => "OpenRouteService";

    public async Task<AddressSearchResult> SearchAddressesAsync(string query, string? provinceName = null, CancellationToken cancellationToken = default)
    {
        var normalized = AddressQueryHelper.NormalizeSegment(query);
        if (string.IsNullOrWhiteSpace(normalized)) return new AddressSearchResult(string.Empty, Array.Empty<AddressSuggestion>());
        var suggestions = await SearchInternalAsync(normalized, provinceName, null, cancellationToken);
        return new AddressSearchResult(normalized, suggestions);
    }

    public Task<GeoPoint?> GeocodeAsync(string address, string? provinceName = null, string? wardName = null, CancellationToken cancellationToken = default)
        => GeocodeInternalAsync(address, provinceName, wardName, cancellationToken);

    private async Task<IReadOnlyList<AddressSuggestion>> SearchInternalAsync(string query, string? provinceName, string? wardName, CancellationToken cancellationToken)
    {
        var requestUri = BuildSearchUri(query, provinceName);
        _logger.LogInformation("ORS search GET {Uri} apiKey={MaskedKey}", MaskUri(requestUri), MaskKey(_apiKey));
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.TryAddWithoutValidation("Authorization", _apiKey);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return Array.Empty<AddressSuggestion>();
        var features = await ParseFeatures(response, cancellationToken);

        var ranked = RankFeatures(query, provinceName, wardName, features);
        return ranked.Where(x => x.Score >= ScoreThreshold).OrderByDescending(x => x.Score).Take(6).Select(x => new AddressSuggestion(x.Label, x.Lat, x.Lon, x.Label)).ToList();
    }

    private async Task<GeoPoint?> GeocodeInternalAsync(string address, string? provinceName, string? wardName, CancellationToken cancellationToken)
    {
        var normalized = AddressQueryHelper.NormalizeSegment(address);
        if (string.IsNullOrWhiteSpace(normalized)) return null;
        var requestUri = BuildSearchUri(normalized, provinceName);
        _logger.LogInformation("ORS geocode GET {Uri} apiKey={MaskedKey}", MaskUri(requestUri), MaskKey(_apiKey));
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.TryAddWithoutValidation("Authorization", _apiKey);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        var features = await ParseFeatures(response, cancellationToken);
        var ranked = RankFeatures(normalized, provinceName, wardName, features);
        var best = ranked.OrderByDescending(x => x.Score).FirstOrDefault();
        if (best == null || best.Score < ScoreThreshold) return null;
        _logger.LogInformation("ORS selected '{Label}' score={Score}", best.Label, best.Score);
        return new GeoPoint(best.Lat, best.Lon);
    }

    public async Task<RouteMetrics?> GetRouteMetricsAsync(GeoPoint origin, GeoPoint destination, CancellationToken cancellationToken = default)
    {
        var body = new { coordinates = new[] { new[] { origin.Longitude, origin.Latitude }, new[] { destination.Longitude, destination.Latitude } } };
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openrouteservice.org/v2/directions/driving-car");
        request.Headers.TryAddWithoutValidation("Authorization", _apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("ORS route failed status={Status} response={Response}", (int)response.StatusCode, payload);
            return null;
        }
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var summary = json.RootElement.GetProperty("routes")[0].GetProperty("summary");
        return new RouteMetrics(Math.Round(summary.GetProperty("distance").GetDecimal() / 1000m, 2), (int)Math.Ceiling(summary.GetProperty("duration").GetDouble() / 60d));
    }

    private Uri BuildSearchUri(string text, string? provinceName)
    {
        var query = new Dictionary<string, string?>
        {
            ["api_key"] = _apiKey,
            ["text"] = text,
            ["size"] = "8",
            ["lang"] = "vi",
            ["boundary_country"] = "VN"
        };
        var province = AddressQueryHelper.Fold(provinceName);
        if (province.Contains("ha noi"))
        {
            query["focus.point.lat"] = "21.0278";
            query["focus.point.lon"] = "105.8342";
        }
        var queryString = query.Where(x => !string.IsNullOrWhiteSpace(x.Value)).ToDictionary(x => x.Key, x => (string?)x.Value);
        return new Uri(QueryHelpers.AddQueryString("https://api.openrouteservice.org/geocode/search", queryString));
    }

    private IEnumerable<RankedFeature> RankFeatures(string query, string? provinceName, string? wardName, IEnumerable<FeatureData> features)
    {
        foreach (var f in features)
        {
            var score = 0;
            var reason = new List<string>();
            if (!IsVietnam(f.Country, f.Label)) { score -= 500; reason.Add("country"); }
            if (!MatchProvince(f, provinceName)) { score -= 200; reason.Add("province_mismatch"); }
            else score += 100;
            if (HasWardOrStreetToken(query, wardName, "hoang mai"))
            {
                if (ContainsAny(f, "hoang mai")) score += 80;
                else { score -= 150; reason.Add("missing_hoang_mai"); }
            }
            if (ContainsStreetInQuery(query))
            {
                var token = ExtractStreetToken(query);
                if (!string.IsNullOrWhiteSpace(token) && ContainsAny(f, token)) score += 50;
                else { score -= 150; reason.Add("street_mismatch"); }
            }
            if (StartsWithHouseNumber(query) && ContainsAny(f, ExtractHouseNumber(query))) score += 20;
            if (ContainsWrongDistrictForWard(f, wardName)) { score -= 150; reason.Add("district_mismatch"); }
            _logger.LogInformation("ORS feature '{Label}' score={Score} region='{Region}' county='{County}' locality='{Locality}' reasons={Reasons}", f.Label, score, f.Region, f.County, f.Locality, string.Join('|', reason));
            yield return new RankedFeature(f.Label, f.Lat, f.Lon, score);
        }
    }

    private static bool ContainsWrongDistrictForWard(FeatureData f, string? ward)
    {
        var w = AddressQueryHelper.Fold(ward);
        if (!w.Contains("hoang mai")) return false;
        return AddressQueryHelper.Fold($"{f.County} {f.Localadmin}").Contains("ha dong");
    }
    private static bool MatchProvince(FeatureData f, string? provinceName)
    {
        var p = AddressQueryHelper.Fold(provinceName);
        if (string.IsNullOrWhiteSpace(p)) return true;
        var hay = AddressQueryHelper.Fold($"{f.Label} {f.Region} {f.Locality} {f.Localadmin}");
        if (p.Contains("ha noi")) return hay.Contains("ha noi") || hay.Contains("hanoi");
        return hay.Contains(p);
    }
    private static bool IsVietnam(string country, string label)
    {
        var c = AddressQueryHelper.Fold($"{country} {label}");
        return c.Contains("viet nam") || c.Contains("vietnam") || c.Contains(" vn ") || c.EndsWith(" vn") || c.StartsWith("vn ");
    }
    private static bool ContainsAny(FeatureData f, string token) => AddressQueryHelper.Fold($"{f.Label} {f.Name} {f.Region} {f.County} {f.Locality} {f.Localadmin} {f.Neighbourhood}").Contains(AddressQueryHelper.Fold(token));
    private static bool StartsWithHouseNumber(string query) => System.Text.RegularExpressions.Regex.IsMatch(AddressQueryHelper.NormalizeSegment(query), "^\\d+");
    private static string ExtractHouseNumber(string query) => System.Text.RegularExpressions.Regex.Match(AddressQueryHelper.NormalizeSegment(query), "^\\d+").Value;
    private static bool ContainsStreetInQuery(string query) => AddressQueryHelper.Fold(query).Contains("duong ") || AddressQueryHelper.Fold(query).Contains("street ");
    private static string ExtractStreetToken(string query) { var q = AddressQueryHelper.Fold(query); var i = q.IndexOf("duong ", StringComparison.Ordinal); return i >= 0 ? q[(i + 6)..].Split(',')[0].Trim() : string.Empty; }
    private static bool HasWardOrStreetToken(string query, string? ward, string token) => AddressQueryHelper.Fold(query).Contains(token) || AddressQueryHelper.Fold(ward).Contains(token);

    private static async Task<List<FeatureData>> ParseFeatures(HttpResponseMessage response, CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (!json.RootElement.TryGetProperty("features", out var features)) return new();
        var result = new List<FeatureData>();
        foreach (var feature in features.EnumerateArray())
        {
            if (!feature.TryGetProperty("properties", out var props) || !feature.TryGetProperty("geometry", out var geo) || !geo.TryGetProperty("coordinates", out var coor) || coor.GetArrayLength() < 2) continue;
            var lon = coor[0].GetDouble(); var lat = coor[1].GetDouble();
            if (lat is < -90 or > 90 || lon is < -180 or > 180) continue;
            result.Add(new FeatureData(
                Get(props, "label"), Get(props, "name"), Get(props, "region"), Get(props, "county"), Get(props, "locality"), Get(props, "localadmin"), Get(props, "neighbourhood"), Get(props, "country"), lat, lon));
        }
        return result;
    }

    private static string Get(JsonElement p, string n) => p.TryGetProperty(n, out var x) ? x.GetString() ?? string.Empty : string.Empty;
    private static string MaskKey(string key) => key.Length <= 8 ? "****" : $"{key[..4]}...{key[^4..]}";
    private static string MaskUri(Uri uri)
    {
        var text = uri.ToString();
        var idx = text.IndexOf("api_key=", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return text;
        var end = text.IndexOf("&", idx, StringComparison.Ordinal);
        if (end < 0) end = text.Length;
        return text[..(idx + 8)] + "***" + text[end..];
    }
    private sealed record FeatureData(string Label, string Name, string Region, string County, string Locality, string Localadmin, string Neighbourhood, string Country, double Lat, double Lon);
    private sealed record RankedFeature(string Label, double Lat, double Lon, int Score);
}
