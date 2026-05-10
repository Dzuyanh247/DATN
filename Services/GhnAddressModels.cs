namespace Datn.PcStore.Services;

public class GhnOptions
{
    public string BaseUrl { get; set; } = "https://online-gateway.ghn.vn/shiip/public-api";
    public string Token { get; set; } = string.Empty;
    public string ShopId { get; set; } = string.Empty;
}

public record ProvinceDto(int Id, string Name);
public record DistrictDto(int Id, string Name, int ProvinceId);
public record WardDto(string Code, string Name, int DistrictId);
