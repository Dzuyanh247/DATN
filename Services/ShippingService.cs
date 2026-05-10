using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Services;

public interface IShippingService
{
    Task<ShippingQuote> CalculateAsync(string shippingAddress, CancellationToken cancellationToken = default);
}

public class ShippingService : IShippingService
{
    private readonly ApplicationDbContext _db;
    private readonly IMapProvider _mapProvider;

    public ShippingService(ApplicationDbContext db, IMapProvider mapProvider)
    {
        _db = db;
        _mapProvider = mapProvider;
    }

    public async Task<ShippingQuote> CalculateAsync(string shippingAddress, CancellationToken cancellationToken = default)
    {
        var config = await _db.ShippingConfigs.FirstOrDefaultAsync(x => x.Active, cancellationToken)
            ?? throw new InvalidOperationException("Chưa cấu hình phí giao hàng.");
        var shop = await _db.ShopLocations.FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Chưa cấu hình vị trí cửa hàng.");

        var destination = await _mapProvider.GeocodeAsync(shippingAddress, cancellationToken)
            ?? throw new InvalidOperationException("Không xác định được tọa độ địa chỉ nhận hàng.");

        var metrics = await _mapProvider.GetRouteMetricsAsync(new GeoPoint(shop.Latitude, shop.Longitude), destination, cancellationToken)
            ?? throw new InvalidOperationException("Không tính được khoảng cách giao hàng.");

        if (metrics.DistanceKm > config.MaxDistanceKm)
        {
            throw new InvalidOperationException("Địa chỉ nằm ngoài phạm vi hỗ trợ giao hàng");
        }

        decimal fee = config.BaseFee;
        if (metrics.DistanceKm > config.BaseDistanceKm)
        {
            var extraKm = (decimal)Math.Ceiling(metrics.DistanceKm - config.BaseDistanceKm);
            fee += extraKm * config.ExtraFeePerKm;
        }

        return new ShippingQuote
        {
            DistanceKm = metrics.DistanceKm,
            DurationMinutes = metrics.DurationMinutes,
            ShippingFee = fee,
            Provider = _mapProvider.ProviderName,
            FormulaSnapshot = $"{config.BaseFee:N0}đ/{config.BaseDistanceKm}km đầu, +{config.ExtraFeePerKm:N0}đ/km tiếp theo, tối đa {config.MaxDistanceKm}km"
        };
    }
}
