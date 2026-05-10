namespace Datn.PcStore.Services;

public record GeoPoint(double Latitude, double Longitude);

public record RouteMetrics(decimal DistanceKm, int DurationMinutes);
public record AddressSuggestion(string DisplayName, double Latitude, double Longitude, string FullAddress);
public record AddressSearchResult(string QueryUsed, IReadOnlyList<AddressSuggestion> Suggestions);

public class ShippingQuote
{
    public double DestinationLatitude { get; set; }
    public double DestinationLongitude { get; set; }
    public decimal DistanceKm { get; set; }
    public int DurationMinutes { get; set; }
    public decimal ShippingFee { get; set; }
    public bool IsFreeShipping { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string FormulaSnapshot { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public decimal? GhnTotal { get; set; }
    public decimal? GhnServiceFee { get; set; }
    public decimal? GhnInsuranceFee { get; set; }
    public long? GhnLeadTime { get; set; }
}

public class ShippingFeeBreakdown
{
    public decimal Fee { get; set; }
    public string FormulaSnapshot { get; set; } = string.Empty;
    public decimal? GhnTotal { get; set; }
    public decimal? GhnServiceFee { get; set; }
    public decimal? GhnInsuranceFee { get; set; }
    public long? GhnLeadTime { get; set; }
}
