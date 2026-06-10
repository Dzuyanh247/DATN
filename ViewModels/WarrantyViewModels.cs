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
}

public class WarrantyProductVm
{
    public int OrderId { get; set; }
    public int OrderDetailId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductImage { get; set; }
    public string? WarrantyCode { get; set; }
    public DateTime PurchaseDate { get; set; }
    public int WarrantyMonths { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsEligibleOrder { get; set; }
    public bool IsInWarranty { get; set; }
    public bool HasActiveRequest { get; set; }
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
    [MaxLength(20)] public string? Phone { get; set; }
    public bool RequiresPhone { get; set; }
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
