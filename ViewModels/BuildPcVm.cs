using Datn.PcStore.Models;

namespace Datn.PcStore.ViewModels;

public class BuildPcVm
{
    public Dictionary<string, List<Product>> Components { get; set; } = new();
    public Dictionary<string, int?> Selected { get; set; } = new();
    public Dictionary<string, Product?> SelectedProducts { get; set; } = new();
    public decimal TotalPrice { get; set; }
    public string CompatibilityMessage { get; set; } = string.Empty;
    public bool IsCompatible { get; set; } = true;
}
