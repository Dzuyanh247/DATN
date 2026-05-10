namespace Datn.PcStore.Services;

public interface IGeocodingService
{
    Task<GeoPoint> GeocodeAsync(string address, CancellationToken cancellationToken = default);
}

public class GeocodingService : IGeocodingService
{
    private readonly IMapProvider _mapProvider;

    public GeocodingService(IMapProvider mapProvider) => _mapProvider = mapProvider;

    public async Task<GeoPoint> GeocodeAsync(string address, CancellationToken cancellationToken = default)
        => await _mapProvider.GeocodeAsync(address, cancellationToken)
           ?? throw new InvalidOperationException("Không xác định được tọa độ địa chỉ nhận hàng.");
}
