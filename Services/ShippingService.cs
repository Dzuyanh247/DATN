using Datn.PcStore.Data;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Services;

public interface IShippingService
{
    Task<ShippingQuote> CalculateAsync(string fullAddress, string provinceName, string districtName, string wardName, CancellationToken cancellationToken = default);
}

public class ShippingService : IShippingService
{
    private readonly ILogger<ShippingService> _logger;
    private readonly ApplicationDbContext _db;
    private readonly IRouteService _routeService;
    private readonly IShippingFeeCalculator _shippingFeeCalculator;
    private readonly IMapProvider _mapProvider;

    public ShippingService(
        ApplicationDbContext db,
        IRouteService routeService,
        IShippingFeeCalculator shippingFeeCalculator,
        IMapProvider mapProvider,
        ILogger<ShippingService> logger)
    {
        _db = db;
        _logger = logger;
        _routeService = routeService;
        _shippingFeeCalculator = shippingFeeCalculator;
        _mapProvider = mapProvider;
    }

    public async Task<ShippingQuote> CalculateAsync(string fullAddress, string provinceName, string districtName, string wardName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fullAddress))
            throw new InvalidOperationException("Vui lòng nhập địa chỉ chi tiết để tính phí giao hàng.");

        var config = await _db.ShippingConfigs.FirstOrDefaultAsync(x => x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Chưa cấu hình phí giao hàng.");

        _logger.LogInformation("ShippingService calculate without geocode province={Province} district={District} ward={Ward}", provinceName, districtName, wardName);
        var feeBreakdown = _shippingFeeCalculator.Calculate(0, config);

        return new ShippingQuote
        {
            DestinationLatitude = 0,
            DestinationLongitude = 0,
            DistanceKm = 0,
            DurationMinutes = 0,
            ShippingFee = feeBreakdown.Fee,
            Provider = "CONFIG_FLAT",
            FormulaSnapshot = feeBreakdown.FormulaSnapshot
        };
    }

    private static GeoPoint NormalizeCoordinate(GeoPoint point)
    {
        var lat = point.Latitude;
        var lng = point.Longitude;
        if (Math.Abs(lat) > 90 && Math.Abs(lat) <= 9000000000d) lat /= 10000000d;
        if (Math.Abs(lng) > 180 && Math.Abs(lng) <= 18000000000d) lng /= 10000000d;
        if (lat is < -90 or > 90 || lng is < -180 or > 180)
            throw new InvalidOperationException("Tọa độ giao hàng không hợp lệ.");
        return new GeoPoint(lat, lng);
    }
}
