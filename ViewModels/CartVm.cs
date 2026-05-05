using Datn.PcStore.Models;

namespace Datn.PcStore.ViewModels;

public class CartLineVm
{
    public int CartItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public string? Warranty { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public int StockQuantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public class CartViewVm
{
    public List<CartLineVm> Items { get; set; } = new();
    public decimal Subtotal => Items.Sum(x => x.LineTotal);
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount => Math.Max(Subtotal - DiscountAmount, 0);
    public string? VoucherCode { get; set; }
}

public class CheckoutRequestVm
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string CustomerAddress { get; set; } = string.Empty;
    public string CustomerProvince { get; set; } = string.Empty;
    public string CustomerDistrict { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? VoucherCode { get; set; }
}
