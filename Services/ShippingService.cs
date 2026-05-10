using Datn.PcStore.Data;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Services;

public interface IShippingService
{
    Task<ShippingQuote> CalculateAsync(int districtId, string wardCode, string provinceName, string districtName, string wardName, int totalWeightGram, int length, int width, int height, CancellationToken cancellationToken = default);
}

public class ShippingService : IShippingService
{
    private readonly ILogger<ShippingService> _logger;
    private readonly ApplicationDbContext _db;
    private readonly IShippingFeeCalculator _shippingFeeCalculator;
    private readonly IGhnShippingService _ghnShippingService;

    public ShippingService(ApplicationDbContext db, IShippingFeeCalculator shippingFeeCalculator, IGhnShippingService ghnShippingService, ILogger<ShippingService> logger)
    {
        _db = db;
        _logger = logger;
        _shippingFeeCalculator = shippingFeeCalculator;
        _ghnShippingService = ghnShippingService;
    }

    public async Task<ShippingQuote> CalculateAsync(int districtId, string wardCode, string provinceName, string districtName, string wardName, int totalWeightGram, int length, int width, int height, CancellationToken cancellationToken = default)
    {
        var config = await _db.ShippingConfigs.FirstOrDefaultAsync(x => x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Chưa cấu hình phí giao hàng.");

        _logger.LogInformation("Shipping calculate province={Province} district={District} ward={Ward} districtId={DistrictId} wardCode={WardCode}", provinceName, districtName, wardName, districtId, wardCode);

        var ghn = await _ghnShippingService.CalculateFeeAsync(districtId, wardCode, totalWeightGram, length, width, height, cancellationToken);
        if (ghn.Success && ghn.ShippingFee >= 0)
        {
            return new ShippingQuote
            {
                ShippingFee = ghn.ShippingFee,
                Provider = "GHN",
                FormulaSnapshot = $"GHN total={ghn.Total ?? ghn.ShippingFee:N0}; service_fee={ghn.ServiceFee?.ToString() ?? "n/a"}; insurance_fee={ghn.InsuranceFee?.ToString() ?? "n/a"}",
                GhnTotal = ghn.Total,
                GhnServiceFee = ghn.ServiceFee,
                GhnInsuranceFee = ghn.InsuranceFee,
                GhnLeadTime = ghn.LeadTime
            };
        }

        var fallback = _shippingFeeCalculator.Calculate(config.BaseDistanceKm, config);
        _logger.LogWarning("GHN fee failed, using fallback fee={Fee} reason={Reason}", fallback.Fee, ghn.ErrorMessage);
        return new ShippingQuote
        {
            ShippingFee = fallback.Fee,
            Provider = "CONFIG_FALLBACK",
            FormulaSnapshot = fallback.FormulaSnapshot
        };
    }
}
