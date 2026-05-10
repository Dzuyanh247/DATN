namespace Datn.PcStore.Services;

public interface IRouteService
{
    Task<RouteMetrics> GetRouteMetricsAsync(GeoPoint origin, GeoPoint destination, CancellationToken cancellationToken = default);
}

public class RouteService : IRouteService
{
    private readonly IMapProvider _mapProvider;

    public RouteService(IMapProvider mapProvider) => _mapProvider = mapProvider;

    public async Task<RouteMetrics> GetRouteMetricsAsync(GeoPoint origin, GeoPoint destination, CancellationToken cancellationToken = default)
        => await _mapProvider.GetRouteMetricsAsync(origin, destination, cancellationToken)
           ?? throw new InvalidOperationException("Không tính được khoảng cách giao hàng.");
}
