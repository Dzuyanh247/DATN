namespace Datn.PcStore.Services;

public interface IMapProvider
{
    Task<GeoPoint?> GeocodeAsync(string address, CancellationToken cancellationToken = default);
    Task<RouteMetrics?> GetRouteMetricsAsync(GeoPoint origin, GeoPoint destination, CancellationToken cancellationToken = default);
    string ProviderName { get; }
}
