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
    {
        ValidateCoordinate(origin, "origin");
        ValidateCoordinate(destination, "destination");
        return await _mapProvider.GetRouteMetricsAsync(origin, destination, cancellationToken)
           ?? throw new InvalidOperationException("Không tính được khoảng cách giao hàng.");
    }

    private static void ValidateCoordinate(GeoPoint point, string name)
    {
        if (point.Latitude is < -90 or > 90 || point.Longitude is < -180 or > 180)
            throw new InvalidOperationException($"Tọa độ {name} không hợp lệ.");
    }
}
