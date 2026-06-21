using System.ComponentModel.DataAnnotations;
using Datn.PcStore.Helpers;
using Datn.PcStore.Models;
using Microsoft.AspNetCore.Http;

namespace Datn.PcStore.ViewModels;

public class WarrantyCheckVm
{
    [Display(Name = "Mã đơn hàng / mã bảo hành / số điện thoại")]
    [MaxLength(80)]
    public string? Query { get; set; }
    public bool HasSearched { get; set; }
    public List<WarrantyProductVm> Products { get; set; } = [];
    public List<WarrantyRequest> Requests { get; set; } = [];
}

public class WarrantyProductVm
{
    public int OrderId { get; set; }
    public int OrderDetailId { get; set; }
    public string OrderCode => $"DH{OrderId:D6}";
    public string? CustomerName { get; set; }
    public string? OrderStatus { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImage { get; set; }
    public string WarrantyCode { get; set; } = string.Empty;
    public string? LookupPhone { get; set; }
    public DateTime PurchaseDate { get; set; }
    public int? WarrantyMonths { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsEligibleOrder { get; set; }
    public WarrantyState WarrantyState { get; set; } = WarrantyState.Contact;
    public bool IsInWarranty => WarrantyState is WarrantyState.Active or WarrantyState.ExpiringSoon;
    public bool HasActiveRequest { get; set; }
    public int WarrantyProgressPercent { get; set; }
    public int WarrantyRemainingPercent => WarrantyMonths.HasValue ? Math.Max(0, 100 - WarrantyProgressPercent) : 0;
    public List<WarrantyComponentVm> Components { get; set; } = [];
    public bool HasComponentWarranty => Components.Count > 0;
    public int TotalComponents => Components.Sum(x => Math.Max(1, x.Quantity));
    public int ComponentsInWarranty => Components.Where(x => x.IsInWarranty).Sum(x => Math.Max(1, x.Quantity));
    public int ComponentsExpired => Components.Where(x => x.State == WarrantyState.Expired).Sum(x => Math.Max(1, x.Quantity));
    public int ComponentsContact => Components.Where(x => x.State == WarrantyState.Contact).Sum(x => Math.Max(1, x.Quantity));
}

public class WarrantyComponentVm
{
    public int Stt { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string? RawWarranty { get; set; }
    public int? WarrantyMonths { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public WarrantyState State { get; set; } = WarrantyState.Contact;
    public int ProgressPercent { get; set; }
    public int RemainingPercent => WarrantyMonths.HasValue ? Math.Max(0, 100 - ProgressPercent) : 0;
    public bool IsInWarranty => State is WarrantyState.Active or WarrantyState.ExpiringSoon;
}

public enum WarrantyState
{
    Active,
    ExpiringSoon,
    Expired,
    Contact
}

public class WarrantyCreateVm
{
    public int OrderDetailId { get; set; }
    public int OrderId { get; set; }
    public string? OrderCode { get; set; }
    public string? ProductName { get; set; }
    public string? ProductImage { get; set; }
    public string? WarrantyCode { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int WarrantyMonths { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [MaxLength(120)]
    [Display(Name = "Họ và tên")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression("^(0|\\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
    [MaxLength(20)]
    [Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [MaxLength(120)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề lỗi.")]
    [MaxLength(200)]
    [Display(Name = "Tiêu đề lỗi")]
    public string IssueTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng mô tả tình trạng sản phẩm.")]
    [MaxLength(2000)]
    [Display(Name = "Mô tả lỗi")]
    public string IssueDescription { get; set; } = string.Empty;

    [Display(Name = "Ảnh minh chứng")]
    public IFormFile? EvidenceImage { get; set; }
}

public class WarrantyMyRequestsVm
{
    [MaxLength(80)] public string? Query { get; set; }
    public bool RequiresLookup { get; set; }
    public bool HasSearched { get; set; }
    public List<WarrantyRequest> Requests { get; set; } = [];
}

public class AdminWarrantyIndexVm
{
    public string? Status { get; set; }
    public string? Search { get; set; }
    public List<WarrantyRequest> Requests { get; set; } = [];
}

public class AdminWarrantyUpdateVm
{
    public int Id { get; set; }

    [Required]
    public string Status { get; set; } = WarrantyStatuses.Pending;

    [MaxLength(2000)]
    [Display(Name = "Ghi chú xử lý")]
    public string? AdminNote { get; set; }
}
