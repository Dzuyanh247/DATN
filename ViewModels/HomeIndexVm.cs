using Datn.PcStore.Models;

namespace Datn.PcStore.ViewModels;

public class HomeIndexVm
{
    public List<Category> Categories { get; set; } = new();
    public List<Banner> Banners { get; set; } = new();
    public SiteSetting? SiteSettings { get; set; }
    public List<Product> HotSaleProducts { get; set; } = new();
    public List<Product> DailyDealProducts { get; set; } = new();
    public List<Product> PromotionProducts { get; set; } = new();
    public List<Product> PcGamingProducts { get; set; } = new();
    public List<Product> LaptopProducts { get; set; } = new();
    public List<Product> MonitorProducts { get; set; } = new();
    public List<Product> ComponentProducts { get; set; } = new();
    public List<Product> WorkstationProducts { get; set; } = new();
    public List<Product> AmdGamingProducts { get; set; } = new();
    public List<Product> PcMiniProducts { get; set; } = new();
    public List<Product> OfficePcProducts { get; set; } = new();
    public List<Product> CpuProducts { get; set; } = new();
    public List<Product> MainboardProducts { get; set; } = new();
    public List<Product> RamProducts { get; set; } = new();
    public List<Product> VgaProducts { get; set; } = new();
    public List<Product> StorageProducts { get; set; } = new();
    public List<Product> PsuProducts { get; set; } = new();
    public List<Product> CaseProducts { get; set; } = new();
    public List<Product> CoolerProducts { get; set; } = new();
    public List<Product> KeyboardProducts { get; set; } = new();
    public List<Product> MouseProducts { get; set; } = new();
    public List<Product> HeadphoneProducts { get; set; } = new();
}
