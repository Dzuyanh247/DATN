using Datn.PcStore.Models;

namespace Datn.PcStore.Services;

public interface IShippingFeeCalculator
{
    ShippingFeeBreakdown Calculate(decimal distanceKm, ShippingConfig config);
}

public class ShippingFeeCalculator : IShippingFeeCalculator
{
    public ShippingFeeBreakdown Calculate(decimal distanceKm, ShippingConfig config)
    {
        if (distanceKm > config.MaxDistanceKm)
            throw new InvalidOperationException("Địa chỉ nằm ngoài phạm vi hỗ trợ giao hàng");

        if (distanceKm <= config.FreeShippingDistanceKm)
            return new ShippingFeeBreakdown { Fee = 0m, FormulaSnapshot = $"Miễn phí giao hàng trong phạm vi {config.FreeShippingDistanceKm}km" };

        if (distanceKm <= config.BaseDistanceKm)
            return new ShippingFeeBreakdown { Fee = config.BaseFee, FormulaSnapshot = $"{config.BaseFee:N0}đ cho {config.BaseDistanceKm}km đầu" };

        var extraDistance = decimal.Ceiling(distanceKm - config.BaseDistanceKm);
        var fee = config.BaseFee + (extraDistance * config.ExtraFeePerKm);
        return new ShippingFeeBreakdown
        {
            Fee = fee,
            FormulaSnapshot = $"{config.BaseFee:N0}đ cho {config.BaseDistanceKm}km đầu + {extraDistance:N0}km x {config.ExtraFeePerKm:N0}đ"
        };
    }
}
