namespace Datn.PcStore.Services;

public record GeoPoint(double Latitude, double Longitude);

public record RouteMetrics(double DistanceKm, double DurationMinutes);

public class ShippingQuote
{
    public double DistanceKm { get; set; }
    public int DurationMinutes { get; set; }
    public decimal ShippingFee { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string FormulaSnapshot { get; set; } = string.Empty;
}
