using Datn.PcStore.Services;

namespace Datn.PcStore.ViewModels;

public class AiChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public string Mode { get; set; } = "ai";
}

public class AiChatProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Specifications { get; set; } = string.Empty;
    public string StockStatus { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    public static AiChatProductDto From(AiProductContext product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Price = product.Price,
        Specifications = product.Specifications,
        StockStatus = product.StockStatus,
        Link = product.Link,
        Category = product.Category
    };
}
