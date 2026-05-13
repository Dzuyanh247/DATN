namespace Datn.PcStore.ViewModels;

public class BuildPcViewModel
{
    public List<BuildPcComponentViewModel> Components { get; set; } = new();
    public decimal TotalPrice => Components.Where(x => x.Selected != null).Sum(x => (x.Selected?.Price ?? 0) * (x.Selected?.Quantity ?? 1));
    public string? CompatibilityWarning { get; set; }
}

public class BuildPcComponentViewModel
{
    public string Type { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public SelectedComponentViewModel? Selected { get; set; }
}

public class SelectedComponentViewModel
{
    public int ProductId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; } = 1;
}

public class BuildProductOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
}
