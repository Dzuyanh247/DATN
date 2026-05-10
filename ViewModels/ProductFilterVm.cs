using Datn.PcStore.Models;

namespace Datn.PcStore.ViewModels;

public class ProductFilterVm
{
    public string? Keyword { get; set; }
    public int? CategoryId { get; set; }
    public string? CategorySlug { get; set; }
    public string? Brand { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public List<Category> Categories { get; set; } = new();
    public List<Product> Products { get; set; } = new();
}
