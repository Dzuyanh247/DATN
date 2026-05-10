namespace Datn.PcStore.Services;

public interface IGeocodingService
{
    Task<GeoPoint> GeocodeAsync(string address, string? provinceName = null, string? wardName = null, CancellationToken cancellationToken = default);
}

public class GeocodingService : IGeocodingService
{
    private readonly IMapProvider _mapProvider;

    public GeocodingService(IMapProvider mapProvider) => _mapProvider = mapProvider;

    public async Task<GeoPoint> GeocodeAsync(string address, string? provinceName = null, string? wardName = null, CancellationToken cancellationToken = default)
        => await _mapProvider.GeocodeAsync(address, provinceName, wardName, cancellationToken)
           ?? throw new InvalidOperationException("Không xác định được tọa độ địa chỉ nhận hàng.");
}
