namespace Datn.PcStore.Services;

public record GeoPoint(double Latitude, double Longitude);

public record RouteMetrics(decimal DistanceKm, int DurationMinutes);
public record AddressSuggestion(string DisplayName, double Latitude, double Longitude, string FullAddress);

public class ShippingQuote
{
    public double DestinationLatitude { get; set; }
    public double DestinationLongitude { get; set; }
    public decimal DistanceKm { get; set; }
    public int DurationMinutes { get; set; }
    public decimal ShippingFee { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string FormulaSnapshot { get; set; } = string.Empty;
}

public class ShippingFeeBreakdown
{
    public decimal Fee { get; set; }
    public string FormulaSnapshot { get; set; } = string.Empty;
}
