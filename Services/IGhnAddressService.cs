namespace Datn.PcStore.Services;

public interface IGhnAddressService
{
    Task<IReadOnlyList<ProvinceDto>> GetProvincesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DistrictDto>> GetDistrictsAsync(int provinceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WardDto>> GetWardsAsync(int districtId, CancellationToken cancellationToken = default);
}
