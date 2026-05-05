using System.ComponentModel.DataAnnotations;
using Datn.PcStore.Models;
using Microsoft.AspNetCore.Http;

namespace Datn.PcStore.ViewModels;

public class AdminProductUpsertVm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn danh mục")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Giá gốc là bắt buộc")]
    [Range(1000, 999999999)]
    public decimal Price { get; set; }

    [Range(0, 999999999)]
    public decimal? DiscountPrice { get; set; }

    [Range(0, 999999999)]
    public int StockQuantity { get; set; }

    [Range(0, 120)]
    public int WarrantyMonths { get; set; } = 12;

    public string Description { get; set; } = string.Empty;
    public string Specifications { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Chỉ upload khi admin chọn file mới; Edit không bắt buộc có ảnh mới.
    public List<IFormFile> NewImages { get; set; } = new();

    public List<int> RemoveImageIds { get; set; } = new();

    // Danh sách id ảnh hiện có theo thứ tự admin sắp xếp (đầu danh sách = ảnh đại diện).
    public List<int> ExistingImageOrder { get; set; } = new();

    public List<ProductImageItemVm> ExistingImages { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
}

public class ProductImageItemVm
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}
