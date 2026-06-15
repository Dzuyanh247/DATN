namespace Datn.PcStore.ViewModels;

public class QuotationViewModel
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public DateTime QuotationDate { get; set; }
    public DateTime OrderDate { get; set; }
    public string ShopName { get; set; } = "KKSHOP";
    public string? ShopAddress { get; set; }
    public string? ShopPhone { get; set; }
    public string? ShopEmail { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public bool IsCancelledOrExpired { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
    public List<QuotationItemViewModel> Items { get; set; } = new();
}

public class QuotationItemViewModel
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Warranty { get; set; }
    public decimal LineTotal { get; set; }
}
