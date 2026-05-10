using Datn.PcStore.Data;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Services;

public interface IShippingService
{
    Task<ShippingQuote> CalculateAsync(string shippingAddress, double? latitude = null, double? longitude = null, CancellationToken cancellationToken = default);
}

public class ShippingService : IShippingService
{
    private readonly ApplicationDbContext _db;
    private readonly IGeocodingService _geocodingService;
    private readonly IRouteService _routeService;
    private readonly IShippingFeeCalculator _shippingFeeCalculator;
    private readonly IMapProvider _mapProvider;

    public ShippingService(
        ApplicationDbContext db,
        IGeocodingService geocodingService,
        IRouteService routeService,
        IShippingFeeCalculator shippingFeeCalculator,
        IMapProvider mapProvider)
    {
        _db = db;
        _geocodingService = geocodingService;
        _routeService = routeService;
        _shippingFeeCalculator = shippingFeeCalculator;
        _mapProvider = mapProvider;
    }

    public async Task<ShippingQuote> CalculateAsync(string shippingAddress, double? latitude = null, double? longitude = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(shippingAddress))
            throw new InvalidOperationException("Vui lòng nhập địa chỉ chi tiết để tính phí giao hàng.");

        var config = await _db.ShippingConfigs.FirstOrDefaultAsync(x => x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Chưa cấu hình phí giao hàng.");
        var shop = await _db.ShopLocations.FirstOrDefaultAsync(x => x.IsDefault, cancellationToken)
            ?? throw new InvalidOperationException("Không lấy được tọa độ shop.");

        GeoPoint destination;
        if (latitude.HasValue && longitude.HasValue)
        {
            destination = new GeoPoint(latitude.Value, longitude.Value);
        }
        else
        {
            destination = await _geocodingService.GeocodeAsync(shippingAddress, cancellationToken);
        }
        var metrics = await _routeService.GetRouteMetricsAsync(new GeoPoint(shop.Latitude, shop.Longitude), destination, cancellationToken);
        var feeBreakdown = _shippingFeeCalculator.Calculate(metrics.DistanceKm, config);

        return new ShippingQuote
        {
            DestinationLatitude = destination.Latitude,
            DestinationLongitude = destination.Longitude,
            DistanceKm = metrics.DistanceKm,
            DurationMinutes = metrics.DurationMinutes,
            ShippingFee = feeBreakdown.Fee,
            Provider = _mapProvider.ProviderName,
            FormulaSnapshot = feeBreakdown.FormulaSnapshot
        };
    }
}
