using Datn.PcStore.Models;

namespace Datn.PcStore.ViewModels;

public class CompareProductVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal OriginalPrice { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public Dictionary<string, string> Specifications { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static CompareProductVm FromProduct(Product product, string imageUrl, Dictionary<string, string> specifications)
    {
        var finalPrice = (product.DiscountPrice ?? product.SalePrice) ?? product.Price;
        return new CompareProductVm
        {
            Id = product.Id,
            Name = product.Name,
            Price = finalPrice,
            OriginalPrice = product.Price,
            ImageUrl = imageUrl,
            Specifications = specifications
        };
    }
}

public class CompareRowVm
{
    public string Label { get; set; } = string.Empty;
    public string ProductAValue { get; set; } = "-";
    public string ProductBValue { get; set; } = "-";
}

public class CompareIndexVm
{
    public List<CompareProductVm> Products { get; set; } = new();
    public List<CompareRowVm> Rows { get; set; } = new();
    public bool HasEnoughProducts => Products.Count == 2;
}
