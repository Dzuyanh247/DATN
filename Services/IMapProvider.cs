namespace Datn.PcStore.Services;

public interface IMapProvider
{
    Task<AddressSearchResult> SearchAddressesAsync(string query, string? provinceName = null, CancellationToken cancellationToken = default);
    Task<GeoPoint?> GeocodeAsync(string address, string? provinceName = null, string? wardName = null, CancellationToken cancellationToken = default);
    Task<RouteMetrics?> GetRouteMetricsAsync(GeoPoint origin, GeoPoint destination, CancellationToken cancellationToken = default);
    string ProviderName { get; }
}
