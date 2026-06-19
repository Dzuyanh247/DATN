using Datn.PcStore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Datn.PcStore.Services;

public interface IShippingService
{
    Task<ShippingQuote> CalculateAsync(int districtId, string wardCode, string provinceName, string districtName, string wardName, string addressDetail, int totalWeightGram, int length, int width, int height, CancellationToken cancellationToken = default);
}

public class ShippingService : IShippingService
{
    private readonly ILogger<ShippingService> _logger;
    private readonly ApplicationDbContext _db;
    private readonly IShippingFeeCalculator _shippingFeeCalculator;
    private readonly IGhnShippingService _ghnShippingService;
    private readonly ShippingPolicyOptions _shippingPolicy;
    private readonly ShopAddressOptions _shopAddress;

    public ShippingService(ApplicationDbContext db, IShippingFeeCalculator shippingFeeCalculator, IGhnShippingService ghnShippingService, IOptions<ShippingPolicyOptions> shippingPolicyOptions, IOptions<ShopAddressOptions> shopAddressOptions, ILogger<ShippingService> logger)
    {
        _db = db;
        _logger = logger;
        _shippingFeeCalculator = shippingFeeCalculator;
        _ghnShippingService = ghnShippingService;
        _shippingPolicy = shippingPolicyOptions.Value;
        _shopAddress = shopAddressOptions.Value;
    }

    public async Task<ShippingQuote> CalculateAsync(int districtId, string wardCode, string provinceName, string districtName, string wardName, string addressDetail, int totalWeightGram, int length, int width, int height, CancellationToken cancellationToken = default)
    {
        var config = await _db.ShippingConfigs.FirstOrDefaultAsync(x => x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Chưa cấu hình phí giao hàng.");

        var sameProvince = IsSameAddressPart(provinceName, _shopAddress.Province);
        var sameDistrict = IsSameAddressPart(districtName, _shopAddress.District);
        var sameWard = IsSameAddressPart(wardName, _shopAddress.Ward);
        var similarDetail = IsSimilarAddressDetail(addressDetail, _shopAddress.AddressDetail);
        var localPolicyMatched = sameWard || (sameProvince && sameDistrict && similarDetail);

        _logger.LogInformation("Shipping calculate province={Province} district={District} ward={Ward} districtId={DistrictId} wardCode={WardCode} sameProvince={SameProvince} sameDistrict={SameDistrict} sameWard={SameWard} similarDetail={SimilarDetail} localPolicyMatched={LocalPolicyMatched}",
            provinceName, districtName, wardName, districtId, wardCode, sameProvince, sameDistrict, sameWard, similarDetail, localPolicyMatched);

        if (localPolicyMatched)
        {
            _logger.LogInformation("Shipping local policy matched but GHN fee calculation remains enabled for checkout verification; GHN skipped={Skipped}", !_shippingPolicy.UseGHNOutsideRadius);
        }

        if (_shippingPolicy.UseGHNOutsideRadius)
        {
            var ghn = await _ghnShippingService.CalculateFeeAsync(districtId, wardCode, totalWeightGram, length, width, height, cancellationToken);
            if (ghn.Success && ghn.ShippingFee >= 0)
            {
                _logger.LogInformation("Shipping fee source={FeeSource}; GHN skipped={Skipped}; finalShippingFee={Fee}", "GHN", false, ghn.ShippingFee);
                return new ShippingQuote
                {
                    ShippingFee = ghn.ShippingFee,
                    IsFreeShipping = ghn.ShippingFee == 0,
                    Provider = "GHN",
                    FormulaSnapshot = $"GHN total={ghn.Total ?? ghn.ShippingFee:N0}; service_fee={ghn.ServiceFee?.ToString() ?? "n/a"}; insurance_fee={ghn.InsuranceFee?.ToString() ?? "n/a"}",
                    Message = "Tính phí giao hàng thành công",
                    GhnTotal = ghn.Total,
                    GhnServiceFee = ghn.ServiceFee,
                    GhnInsuranceFee = ghn.InsuranceFee,
                    GhnLeadTime = ghn.LeadTime
                };
            }
        }

        var policyConfig = new Datn.PcStore.Models.ShippingConfig
        {
            BaseDistanceKm = 3m,
            BaseFee = _shippingPolicy.BaseFee,
            ExtraFeePerKm = _shippingPolicy.ExtraFeePerKm,
            MaxDistanceKm = _shippingPolicy.MaxDistanceKm,
            FreeShippingDistanceKm = _shippingPolicy.FreeShippingRadiusKm
        };
        if (localPolicyMatched)
        {
            _logger.LogInformation("Shipping fee source={FeeSource}; GHN skipped={Skipped}; finalShippingFee={Fee}", "LocalFreeRadius", true, 0);
            return new ShippingQuote
            {
                ShippingFee = 0m,
                IsFreeShipping = true,
                Provider = "LocalFreeRadius",
                FormulaSnapshot = $"Miễn phí giao hàng trong phạm vi {_shippingPolicy.FreeShippingRadiusKm}km",
                Message = $"Miễn phí giao hàng trong phạm vi {_shippingPolicy.FreeShippingRadiusKm}km"
            };
        }

        var fallbackDistance = sameProvince && sameDistrict ? _shippingPolicy.FreeShippingRadiusKm : 5m;
        var fallback = _shippingFeeCalculator.Calculate(fallbackDistance, policyConfig);
        _logger.LogWarning("Shipping fallback feeSource={FeeSource}; finalShippingFee={Fee}", "LocalFormulaFallback", fallback.Fee);
        return new ShippingQuote
        {
            ShippingFee = fallback.Fee,
            IsFreeShipping = fallback.Fee == 0,
            Provider = "LocalFormulaFallback",
            FormulaSnapshot = fallback.FormulaSnapshot,
            Message = "Áp dụng phí ship nội bộ"
        };
    }

    private static bool IsSameAddressPart(string source, string target) => Normalize(source) == Normalize(target);
    private static bool IsSimilarAddressDetail(string source, string target)
    {
        var normalizedSource = Normalize(source);
        var normalizedTarget = Normalize(target);
        return normalizedSource.Contains(normalizedTarget) || normalizedTarget.Contains(normalizedSource);
    }
    private static string Normalize(string value) => (value ?? string.Empty).Trim().ToLowerInvariant();
}
