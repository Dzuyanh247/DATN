using Datn.PcStore.ViewModels;

namespace Datn.PcStore.Services;

public class ShowroomService : IShowroomService
{
    private const string DefaultOpeningHours = "Từ 8h30-20h00 hằng ngày";
    public Task<IReadOnlyList<ShowroomMenuItemVm>> GetHeaderShowroomsAsync() =>
        Task.FromResult(GetFallbackShowrooms());

    private static IReadOnlyList<ShowroomMenuItemVm> GetFallbackShowrooms() =>
    [
        new("CHI NHÁNH ĐỐNG ĐA - HÀ NỘI", "83-85 Thái Hà, Trung Liệt, Đống Đa, Hà Nội", "036.625.8142", DefaultOpeningHours, 21.0121923, 105.8205101),
        new("CHI NHÁNH QUẬN 10 - Hồ Chí Minh", "83A Cửu Long, Phường 15, Quận 10, TP Hồ Chí Minh", "098.668.0497", DefaultOpeningHours, 10.7769, 106.6687)
    ];
}
