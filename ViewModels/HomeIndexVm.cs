using Datn.PcStore.Models;

namespace Datn.PcStore.ViewModels;

public class HomeIndexVm
{
    public List<Category> Categories { get; set; } = new();
    public List<Banner> Banners { get; set; } = new();
    public SiteSetting? SiteSettings { get; set; }
    public List<Product> FeaturedProducts { get; set; } = new();
    public List<Product> PromotionProducts { get; set; } = new();
    public List<Product> PcGamingProducts { get; set; } = new();
    public List<Product> LaptopProducts { get; set; } = new();
    public List<Product> MonitorProducts { get; set; } = new();
    public List<Product> ComponentProducts { get; set; } = new();
}
