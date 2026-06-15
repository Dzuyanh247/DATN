using Datn.PcStore.Models;

namespace Datn.PcStore.ViewModels;

public class AdminInvoicesVm
{
    public string? Search { get; init; }
    public string? PaymentStatus { get; init; }
    public IReadOnlyList<AdminInvoiceRowVm> Invoices { get; init; } = [];
}

public class AdminInvoiceRowVm
{
    public int OrderId { get; init; }
    public string InvoiceCode => $"HD{OrderId:D6}";
    public string OrderCode => $"DH{OrderId:D6}";
    public string CustomerName { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string PaymentStatus { get; init; } = string.Empty;
    public OrderStatus OrderStatus { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsPrintable => OrderStatus is not OrderStatus.Cancelled and not OrderStatus.Expired;
}
