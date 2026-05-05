using System.ComponentModel.DataAnnotations;

namespace Datn.PcStore.ViewModels;

public class CheckoutVm
{
    [Required] public string ReceiverName { get; set; } = string.Empty;
    [Required] public string ReceiverPhone { get; set; } = string.Empty;
    [Required] public string ShippingAddress { get; set; } = string.Empty;
}
