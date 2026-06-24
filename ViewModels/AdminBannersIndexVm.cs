namespace Datn.PcStore.ViewModels;

public class AdminBannersIndexVm
{
    public List<Models.Banner> Banners { get; set; } = new();
    public AdminSiteSettingsVm Settings { get; set; } = new();
}
