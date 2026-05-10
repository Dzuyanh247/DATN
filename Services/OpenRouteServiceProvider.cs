using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Datn.PcStore.Services;

public class OpenRouteServiceProvider : IMapProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenRouteServiceProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OPENROUTESERVICE_API_KEY"]
            ?? configuration["OpenRouteService:ApiKey"]
            ?? throw new InvalidOperationException("Thiếu cấu hình OPENROUTESERVICE_API_KEY.");
    }

    public string ProviderName => "OpenRouteService";

    public async Task<IReadOnlyList<AddressSuggestion>> SearchAddressesAsync(string query, string? provinceName = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<AddressSuggestion>();

        var cityHint = provinceName?.Trim();
        var text = string.IsNullOrWhiteSpace(cityHint) ? query : $"{query}, {cityHint}";
        var body = new
        {
            text,
            size = 6,
            layers = new[] { "address", "street", "venue", "locality" },
            boundary_country = new[] { "VN" },
            lang = "vi"
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openrouteservice.org/geocode/autocomplete");
        request.Headers.TryAddWithoutValidation("Authorization", _apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return Array.Empty<AddressSuggestion>();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var results = new List<AddressSuggestion>();
        if (!json.RootElement.TryGetProperty("features", out var features)) return results;

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

        if (!string.IsNullOrWhiteSpace(provinceName) && provinceName.Contains("Hà Nội", StringComparison.OrdinalIgnoreCase))
        {
            return results
                .OrderByDescending(x => x.DisplayName.Contains("Hà Nội", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return results;
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
