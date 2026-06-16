using Datn.PcStore.Models;

namespace Datn.PcStore.ViewModels;

public class ProductDetailViewModel
{
    public Product Product { get; set; } = new();
    public List<Product> Monitors { get; set; } = new();
    public List<Product> Keyboards { get; set; } = new();
    public List<Product> Mice { get; set; } = new();
    public List<Product> Headsets { get; set; } = new();
}
