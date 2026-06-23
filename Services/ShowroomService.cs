using Datn.PcStore.Data;
using Datn.PcStore.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Services;

public class ShowroomService : IShowroomService
{
    private const string DefaultHotline = "0375 570 025";
    private const string DefaultOpeningHours = "08:30 - 20:00";
    private readonly ApplicationDbContext _db;

    public ShowroomService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ShowroomMenuItemVm>> GetHeaderShowroomsAsync()
    {
        var locations = await _db.ShopLocations
            .AsNoTracking()
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Id)
            .Select(x => new ShowroomMenuItemVm(
                x.ShopName,
                x.Address ?? string.Empty,
                string.IsNullOrWhiteSpace(x.Hotline) ? DefaultHotline : x.Hotline,
                string.IsNullOrWhiteSpace(x.OpeningHours) ? DefaultOpeningHours : x.OpeningHours,
                x.Latitude,
                x.Longitude))
            .ToListAsync();

        return locations.Count > 0 ? locations : GetFallbackShowrooms();
    }

    private static IReadOnlyList<ShowroomMenuItemVm> GetFallbackShowrooms() =>
    [
        new("SHOWROOM HÀ NỘI", "Số 01 Đống Đa, Hà Nội", DefaultHotline, DefaultOpeningHours, 21.0121923, 105.8205101),
        new("SHOWROOM TP. HỒ CHÍ MINH", "Số 02 Quận 10, TP. Hồ Chí Minh", DefaultHotline, DefaultOpeningHours, 10.7769, 106.6687)
    ];
}
