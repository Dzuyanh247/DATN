using Datn.PcStore.Data;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Services;

public interface IShippingService
{
    Task<ShippingQuote> CalculateAsync(string shippingAddress, double? latitude = null, double? longitude = null, CancellationToken cancellationToken = default);
}

public class ShippingService : IShippingService
{
    private readonly ILogger<ShippingService> _logger;
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
        IMapProvider mapProvider,
        ILogger<ShippingService> logger)
    {
        _db = db;
        _logger = logger;
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

        _logger.LogInformation("ShippingService calculate address='{Address}' hasCoordinates={HasCoordinates}", shippingAddress, latitude.HasValue && longitude.HasValue);

        GeoPoint destination;
        if (latitude.HasValue && longitude.HasValue)
        {
            destination = new GeoPoint(latitude.Value, longitude.Value);
            _logger.LogInformation("ShippingService using provided coordinates lat={Lat} lng={Lng}", destination.Latitude, destination.Longitude);
        }
        else
        {
            _logger.LogInformation("ShippingService fallback geocode address='{Address}'", shippingAddress);
            destination = await _geocodingService.GeocodeAsync(shippingAddress, cancellationToken);
            _logger.LogInformation("ShippingService geocode result lat={Lat} lng={Lng}", destination.Latitude, destination.Longitude);
        }
        var metrics = await _routeService.GetRouteMetricsAsync(new GeoPoint(shop.Latitude, shop.Longitude), destination, cancellationToken);
        _logger.LogInformation("ShippingService route result distanceKm={DistanceKm} durationMinutes={DurationMinutes}", metrics.DistanceKm, metrics.DurationMinutes);
        var feeBreakdown = _shippingFeeCalculator.Calculate(metrics.DistanceKm, config);

        var quote = new ShippingQuote
        {
            DestinationLatitude = destination.Latitude,
            DestinationLongitude = destination.Longitude,
            DistanceKm = metrics.DistanceKm,
            DurationMinutes = metrics.DurationMinutes,
            ShippingFee = feeBreakdown.Fee,
            Provider = _mapProvider.ProviderName,
            FormulaSnapshot = feeBreakdown.FormulaSnapshot
        };

        _logger.LogInformation("ShippingService final fee={Fee} formula={Formula}", quote.ShippingFee, quote.FormulaSnapshot);
        return quote;
    }
}
