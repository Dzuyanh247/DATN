namespace Datn.PcStore.Services;

public record GeoPoint(double Latitude, double Longitude);

public record RouteMetrics(decimal DistanceKm, int DurationMinutes);

public class ShippingQuote
{
    public decimal DistanceKm { get; set; }
    public int DurationMinutes { get; set; }
    public decimal ShippingFee { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string FormulaSnapshot { get; set; } = string.Empty;
}
