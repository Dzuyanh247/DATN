using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;

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
        _logger.LogInformation("ORS address search raw_query='{RawQuery}' normalized_query='{NormalizedQuery}' province='{Province}'", query, normalizedQuery, provinceName);

        var fallbackQueries = BuildFallbackQueries(normalizedQuery, cleanedNoComma, provinceName);
        _logger.LogInformation("ORS fallback query candidates count={Count}", fallbackQueries.Count);
        foreach (var candidate in fallbackQueries)
        {
            var suggestions = await SearchInternalAsync(candidate, cancellationToken);
            if (suggestions.Count > 0)
                return new AddressSearchResult(candidate, suggestions);
        }

        return new AddressSearchResult(fallbackQueries.LastOrDefault() ?? normalizedQuery, Array.Empty<AddressSuggestion>());
    }

    private async Task<IReadOnlyList<AddressSuggestion>> SearchInternalAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<AddressSuggestion>();

        var body = new
        {
            text = query,
            size = 6,
            lang = "vi"
        };

        const string endpoint = "https://api.openrouteservice.org/geocode/search";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.TryAddWithoutValidation("Authorization", _apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        _logger.LogInformation("ORS request endpoint='{Endpoint}' query_sent='{QuerySent}' payload={@Payload}", endpoint, query, body);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        _logger.LogInformation("ORS geocode/search status={StatusCode}", (int)response.StatusCode);
        if (!response.IsSuccessStatusCode) return Array.Empty<AddressSuggestion>();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var results = new List<AddressSuggestion>();
        if (!json.RootElement.TryGetProperty("features", out var features)) return results;
        _logger.LogInformation("ORS geocode/search feature_count={FeatureCount}", features.GetArrayLength());

        foreach (var feature in features.EnumerateArray())
        {
            if (!feature.TryGetProperty("geometry", out var geometry) || !geometry.TryGetProperty("coordinates", out var coordinates) || coordinates.GetArrayLength() < 2)
                continue;

            var lon = coordinates[0].GetDouble();
            var lat = coordinates[1].GetDouble();
            var label = feature.GetProperty("properties").GetProperty("label").GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(label)) continue;

            results.Add(new AddressSuggestion(label, lat, lon, label));
        }

        _logger.LogInformation("ORS geocode/search mapped_suggestions_count={Count}", results.Count);
        return results;
    }

    private static IReadOnlyList<string> BuildFallbackQueries(string normalizedQuery, string cleanedNoComma, string? provinceName)
    {
        var removedDiacritics = RemoveDiacritics(cleanedNoComma);
        var noHouseNumber = NormalizeQuery(Regex.Replace(cleanedNoComma, "^\\d+\\s*", string.Empty));
        var province = NormalizeQuery(provinceName ?? string.Empty);
        var candidates = new List<string>
        {
            normalizedQuery,
            cleanedNoComma,
            removedDiacritics,
            string.IsNullOrWhiteSpace(province) ? string.Empty : $"{cleanedNoComma}, {province}, Vietnam",
            $"{cleanedNoComma} Vietnam",
            $"{cleanedNoComma} Ha Noi Vietnam",
            noHouseNumber
        };
        return candidates
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(NormalizeQuery)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeQuery(string query)
        => Regex.Replace(query.Trim(), "\\s+", " ");

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd')
            .Replace('Đ', 'D');
    }

    public async Task<GeoPoint?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        var body = new
        {
            text = address,
            size = 1,
            boundary_country = new[] { "VN" },
            lang = "vi"
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openrouteservice.org/geocode/search");
        request.Headers.TryAddWithoutValidation("Authorization", _apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        _logger.LogInformation("ORS geocode status={StatusCode} query={Query}", (int)response.StatusCode, address);
        if (!response.IsSuccessStatusCode) return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var first = json.RootElement.GetProperty("features").EnumerateArray().FirstOrDefault();
        if (first.ValueKind == JsonValueKind.Undefined) return null;
        var coordinates = first.GetProperty("geometry").GetProperty("coordinates");
        if (coordinates.GetArrayLength() < 2) return null;

        return new GeoPoint(coordinates[1].GetDouble(), coordinates[0].GetDouble());
    }

    public async Task<RouteMetrics?> GetRouteMetricsAsync(GeoPoint origin, GeoPoint destination, CancellationToken cancellationToken = default)
    {
        var body = new
        {
            coordinates = new[]
            {
                new[] { origin.Longitude, origin.Latitude },
                new[] { destination.Longitude, destination.Latitude }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openrouteservice.org/v2/directions/driving-car");
        request.Headers.TryAddWithoutValidation("Authorization", _apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        _logger.LogInformation("ORS route status={StatusCode} origin=({OriginLat},{OriginLng}) destination=({DestLat},{DestLng})", (int)response.StatusCode, origin.Latitude, origin.Longitude, destination.Latitude, destination.Longitude);
        if (!response.IsSuccessStatusCode) return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var summary = json.RootElement.GetProperty("routes")[0].GetProperty("summary");
        var distanceMeters = summary.GetProperty("distance").GetDecimal();
        var durationSeconds = summary.GetProperty("duration").GetDouble();
        var distanceKm = decimal.Round(distanceMeters / 1000m, 2, MidpointRounding.AwayFromZero);
        var durationMinutes = (int)Math.Ceiling(durationSeconds / 60d);

        return new RouteMetrics(distanceKm, durationMinutes);
    }
}
