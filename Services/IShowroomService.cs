using Datn.PcStore.ViewModels;

namespace Datn.PcStore.Services;

public interface IShowroomService
{
    Task<IReadOnlyList<ShowroomMenuItemVm>> GetHeaderShowroomsAsync();
}
