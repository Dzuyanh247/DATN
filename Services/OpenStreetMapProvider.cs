using System.Text.Json;

namespace Datn.PcStore.Services;

public class OpenStreetMapProvider : IMapProvider
{
    private readonly HttpClient _httpClient;

    public OpenStreetMapProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string ProviderName => "OpenStreetMap(Nominatim+OSRM)";

    public async Task<GeoPoint?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        var query = Uri.EscapeDataString(address);
        using var response = await _httpClient.GetAsync($"https://nominatim.openstreetmap.org/search?q={query}&format=jsonv2&limit=1", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var first = json.RootElement.EnumerateArray().FirstOrDefault();
        if (first.ValueKind == JsonValueKind.Undefined) return null;

        if (!double.TryParse(first.GetProperty("lat").GetString(), out var lat)) return null;
        if (!double.TryParse(first.GetProperty("lon").GetString(), out var lon)) return null;
        return new GeoPoint(lat, lon);
    }

    public async Task<RouteMetrics?> GetRouteMetricsAsync(GeoPoint origin, GeoPoint destination, CancellationToken cancellationToken = default)
    {
        var url = $"https://router.project-osrm.org/route/v1/driving/{origin.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{origin.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)};{destination.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{destination.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}?overview=false";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var routes = json.RootElement.GetProperty("routes");
        var first = routes.EnumerateArray().FirstOrDefault();
        if (first.ValueKind == JsonValueKind.Undefined) return null;

        var distanceMeters = first.GetProperty("distance").GetDouble();
        var durationSeconds = first.GetProperty("duration").GetDouble();
        return new RouteMetrics(Math.Round(distanceMeters / 1000d, 2), (int)Math.Ceiling(durationSeconds / 60d));
    }
}
