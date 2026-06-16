using System.ComponentModel.DataAnnotations;
using Datn.PcStore.Models;

namespace Datn.PcStore.ViewModels;

public class AdminProductUpsertVm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn danh mục")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục hợp lệ")]
    public int? CategoryId { get; set; }

    [MaxLength(80)]
    public string? Brand { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại sản phẩm / linh kiện")]
    [MaxLength(40)]
    public string ComponentType { get; set; } = "PC";

    public string ProductType { get; set; } = ProductKinds.PC;

    public List<string> ComponentTypeOptions { get; set; } = ProductTypeOptions.All;

    [Required(ErrorMessage = "Giá gốc là bắt buộc")]
    [Range(1000, 999999999, ErrorMessage = "Giá gốc phải từ 1.000 đến 999.999.999")]
    public decimal? Price { get; set; }

    [Range(0, 999999999, ErrorMessage = "Giá khuyến mãi không được âm")]
    public decimal? DiscountPrice { get; set; }
    public bool IsHotSale { get; set; }
    public bool IsDailyDeal { get; set; }
    public bool IsPromotion { get; set; }
    public DateTime? PromotionStartDate { get; set; }
    public DateTime? PromotionEndDate { get; set; }

    public List<string> SelectedPromotionTexts { get; set; } = new();

    [MaxLength(2000, ErrorMessage = "Nội dung khuyến mại không được vượt quá 2.000 ký tự")]
    public string? CustomPromotionText { get; set; }

    [Required(ErrorMessage = "Số lượng tồn kho là bắt buộc")]
    [Range(0, 999999999, ErrorMessage = "Số lượng tồn kho không được âm")]
    public int? StockQuantity { get; set; }

    [Required(ErrorMessage = "Thời gian bảo hành là bắt buộc")]
    [Range(0, 120, ErrorMessage = "Bảo hành phải từ 0 đến 120 tháng")]
    public int? WarrantyMonths { get; set; } = 12;

    public string? Description { get; set; }
    public string? Specifications { get; set; }
    public List<ProductComponentSpecViewModel> ComponentSpecs { get; set; } = new();

    public bool IsActive { get; set; } = true;

    [Url(ErrorMessage = "URL thumbnail không hợp lệ")]
    [MaxLength(1000)]
    public string? ThumbnailImageUrl { get; set; }

    public string? ProductImageUrlsText { get; set; }

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

public class ProductComponentSpecViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Thứ tự cấu hình phải lớn hơn 0")]
    public int? Stt { get; set; }

    [Required(ErrorMessage = "Linh kiện / thông số không được để trống")]
    public string? Description { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Số lượng cấu hình phải lớn hơn 0")]
    public int? Quantity { get; set; } = 1;

    public string? Warranty { get; set; }
}

public static class ProductTypeOptions
{
    public static readonly List<string> All = new()
    {
        "PC", "Laptop", "Khác"
    };
}


public class AdminComponentIndexVm
{
    public List<Product> Components { get; set; } = new();
    public List<string> ComponentTypeOptions { get; set; } = ComponentTypes.All;
    public List<string> BrandOptions { get; set; } = new();
    public string? Keyword { get; set; }
    public string? ComponentType { get; set; }
    public string? Brand { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? IsActive { get; set; }
    public bool? InStock { get; set; }
}

public class AdminComponentUpsertVm : AdminProductUpsertVm
{
    public AdminComponentUpsertVm()
    {
        ProductType = ProductKinds.Component;
        ComponentType = ComponentTypes.Other;
        ComponentTypeOptions = ComponentTypes.All;
    }

    [Required(ErrorMessage = "Vui lòng chọn loại linh kiện")]
    [MaxLength(40)]
    public new string ComponentType { get; set; } = ComponentTypes.Other;
}
