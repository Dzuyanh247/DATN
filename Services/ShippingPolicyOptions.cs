namespace Datn.PcStore.Services;

public class ShippingPolicyOptions
{
    public decimal FreeShippingRadiusKm { get; set; } = 10m;
    public decimal BaseFee { get; set; } = 15000m;
    public decimal ExtraFeePerKm { get; set; } = 5000m;
    public decimal MaxDistanceKm { get; set; } = 15m;
    public bool UseGHNOutsideRadius { get; set; } = true;
}

public class ShopAddressOptions
{
    public string Province { get; set; } = "Hà Nội";
    public string District { get; set; } = "Quận Hoàng Mai";
    public string Ward { get; set; } = "Phường Hoàng Văn Thụ";
    public string AddressDetail { get; set; } = "257 đường Hoàng Mai";
}
