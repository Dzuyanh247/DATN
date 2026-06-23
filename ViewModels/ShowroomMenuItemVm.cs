namespace Datn.PcStore.ViewModels;

public sealed record ShowroomMenuItemVm(
    string Name,
    string Address,
    string Hotline,
    string OpeningHours,
    double Latitude,
    double Longitude)
{
    public string GoogleMapsUrl => $"https://maps.google.com/?q={Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
}
