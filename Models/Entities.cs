using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Datn.PcStore.Models;

public static class ProductKinds
{
    public const string PC = "PC";
    public const string Component = "Component";
}

public static class ComponentTypes
{
    public const string CPU = "CPU";
    public const string Mainboard = "Mainboard";
    public const string RAM = "RAM";
    public const string VGA = "VGA";
    public const string Storage = "Storage";
    public const string PSU = "PSU";
    public const string Case = "Case";
    public const string Cooler = "Cooler";
    public const string Monitor = "Monitor";
    public const string Keyboard = "Keyboard";
    public const string Mouse = "Mouse";
    public const string Headphone = "Headphone";
    public const string MonitorArm = "MonitorArm";
    public const string Other = "Other";

    public static readonly List<string> All = new()
    {
        CPU, Mainboard, RAM, VGA, Storage, PSU, Case, Cooler,
        Monitor, Keyboard, Mouse, Headphone, MonitorArm, Other
    };

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [CPU] = "CPU",
        [Mainboard] = "Mainboard - Bo mạch chủ",
        [RAM] = "RAM",
        [VGA] = "VGA - Card màn hình",
        [Storage] = "Storage - SSD/HDD",
        [PSU] = "PSU - Nguồn máy tính",
        [Case] = "Case - Vỏ case",
        [Cooler] = "Cooler - Tản nhiệt",
        [Monitor] = "Monitor - Màn hình",
        [Keyboard] = "Keyboard - Bàn phím",
        [Mouse] = "Mouse - Chuột",
        [Headphone] = "Headphone - Tai nghe",
        [MonitorArm] = "MonitorArm - Giá treo màn hình",
        [Other] = "Other - Khác"
    };

    public static readonly IReadOnlyDictionary<string, string> Slugs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [CPU] = "cpu", [Mainboard] = "mainboard", [RAM] = "ram", [VGA] = "vga", [Storage] = "storage",
        [PSU] = "psu", [Case] = "case", [Cooler] = "cooler", [Monitor] = "monitor", [Keyboard] = "keyboard",
        [Mouse] = "mouse", [Headphone] = "headphone", [MonitorArm] = "monitor-arm", [Other] = "other"
    };

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Other;
        var raw = value.Trim();
        var compact = RemoveVietnameseDiacritics(raw).ToLowerInvariant()
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace("/", string.Empty)
            .Replace(" ", string.Empty);

        if (compact.Contains("mainboard") || compact.Contains("bomachchu") || compact.Contains("motherboard")) return Mainboard;
        if (compact == "cpu" || compact.Contains("bovixuly") || compact.Contains("processor")) return CPU;
        if (compact.Contains("ram") || compact.Contains("bonhotrong")) return RAM;
        if (compact.Contains("vga") || compact.Contains("cardmanhinh") || compact.Contains("gpu")) return VGA;
        if (compact.Contains("storage") || compact.Contains("ssd") || compact.Contains("hdd") || compact.Contains("ocung")) return Storage;
        if (compact.Contains("psu") || compact.Contains("nguon")) return PSU;
        if (compact.Contains("case") || compact.Contains("vocase")) return Case;
        if (compact.Contains("cooler") || compact.Contains("tannhiet")) return Cooler;
        if (compact.Contains("monitorarm") || compact.Contains("giatreomanhinh")) return MonitorArm;
        if (compact.Contains("monitor") || compact.Contains("manhinh")) return Monitor;
        if (compact.Contains("keyboard") || compact.Contains("banphim")) return Keyboard;
        if (compact.Contains("mouse") || compact.Contains("chuot")) return Mouse;
        if (compact.Contains("headphone") || compact.Contains("tainghe") || compact.Contains("headset")) return Headphone;
        return All.FirstOrDefault(type => string.Equals(type, raw, StringComparison.OrdinalIgnoreCase)) ?? Other;
    }

    public static string GetLabel(string? value) => Labels.TryGetValue(Normalize(value), out var label) ? label : Labels[Other];
    public static string GetSlug(string? value) => Slugs.TryGetValue(Normalize(value), out var slug) ? slug : Slugs[Other];

    private static string RemoveVietnameseDiacritics(string value)
    {
        var normalized = value.Replace('đ', 'd').Replace('Đ', 'D').Normalize(System.Text.NormalizationForm.FormD);
        var chars = normalized.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray();
        return new string(chars).Normalize(System.Text.NormalizationForm.FormC);
    }
}

public class Role : BaseEntity
{
    [MaxLength(50)] public string Name { get; set; } = string.Empty;
    public ICollection<User> Users { get; set; } = new List<User>();
}

public class User : BaseEntity
{
    [MaxLength(60)] public string Username { get; set; } = string.Empty;
    [MaxLength(100)] public string FullName { get; set; } = string.Empty;
    [MaxLength(120)] public string Email { get; set; } = string.Empty;
    [MaxLength(500)] public string PasswordHash { get; set; } = string.Empty;
    [MaxLength(20)] public string Phone { get; set; } = string.Empty;
    [MaxLength(250)] public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int RoleId { get; set; }
    public Role? Role { get; set; }
    public ICollection<PasswordResetOtp> PasswordResetOtps { get; set; } = new List<PasswordResetOtp>();
}

public class PasswordResetOtp
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    [MaxLength(120)] public string Email { get; set; } = string.Empty;
    [MaxLength(128)] public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UsedAt { get; set; }
}

public class Category : BaseEntity
{
    [MaxLength(120)] public string Name { get; set; } = string.Empty;
    [MaxLength(50)] public string IconClass { get; set; } = "bi bi-grid";
    public int? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
}

public class ComponentBrand : BaseEntity
{
    [MaxLength(80)] public string Name { get; set; } = string.Empty;
    [MaxLength(40)] public string ComponentType { get; set; } = ComponentTypes.Other;
}

public class Product : BaseEntity
{
    [MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(220)] public string Slug { get; set; } = string.Empty;
    [MaxLength(50)] public string ProductCode { get; set; } = string.Empty;
    [MaxLength(80)] public string? Brand { get; set; }
    [MaxLength(20)] public string? ProductType { get; set; } = ProductKinds.PC;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public decimal? SalePrice { get; set; }
    public bool IsHotSale { get; set; }
    public bool IsDailyDeal { get; set; }
    public bool IsPromotion { get; set; }
    public DateTime? PromotionStartDate { get; set; }
    public DateTime? PromotionEndDate { get; set; }
    [MaxLength(2000)] public string? PromotionText { get; set; }
    public int StockQuantity { get; set; }
    [MaxLength(1000)] public string? ThumbnailImage { get; set; } = string.Empty;
    [MaxLength(1000)] public string SourceUrl { get; set; } = string.Empty;
    [MaxLength(500)] public string? ShortDescription { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public string? DetailDescription { get; set; } = string.Empty;
    public string? Specifications { get; set; } = string.Empty;
    [NotMapped]
    public string TechnicalSpecifications
    {
        get => Specifications ?? string.Empty;
        set => Specifications = value ?? string.Empty;
    }
    public int WarrantyMonths { get; set; } = 12;
    [MaxLength(50)] public string WarrantyDuration { get; set; } = "12 tháng";
    public bool IsActive { get; set; } = true;
    public bool IsInStock { get; set; } = true;
    public bool HasSoftwareLicense { get; set; }
    [MaxLength(40)] public string? ComponentType { get; set; } = ComponentTypes.Other;
    [MaxLength(20)] public string? CpuSocket { get; set; }
    [MaxLength(20)] public string? RamType { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
}

public class Banner : BaseEntity
{
    [Required(ErrorMessage = "Tên banner không được để trống")]
    [MaxLength(160)] public string Title { get; set; } = string.Empty;
    [MaxLength(1000)] public string ImageUrl { get; set; } = string.Empty;
    [MaxLength(1000)] public string LinkUrl { get; set; } = "/Products";
    [MaxLength(500)] public string Description { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng chọn vị trí hiển thị")]
    [MaxLength(50)] public string Position { get; set; } = "MainBanner";
    [Range(0, int.MaxValue, ErrorMessage = "Thứ tự hiển thị phải từ 0 trở lên")]
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Cart : BaseEntity
{
    public int UserId { get; set; }
    public User? User { get; set; }
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}

public class CartItem : BaseEntity
{
    public int CartId { get; set; }
    public Cart? Cart { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
}

public enum OrderStatus { Pending = 1, Processing, Delivering, Completed, Cancelled, PendingConfirmation, PendingPayment, Expired }

public class Order : BaseEntity
{
    public int? UserId { get; set; }
    public User? User { get; set; }
    [MaxLength(120)] public string ReceiverName { get; set; } = string.Empty;
    [MaxLength(20)] public string ReceiverPhone { get; set; } = string.Empty;
    [MaxLength(250)] public string ShippingAddress { get; set; } = string.Empty;
    [MaxLength(30)] public string PaymentMethod { get; set; } = "COD";
    [MaxLength(30)] public string PaymentStatus { get; set; } = "UNPAID";
    [MaxLength(50)] public string? TransferContent { get; set; }
    public DateTime? PaidAt { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    [MaxLength(120)] public string? CustomerEmail { get; set; }
    [MaxLength(100)] public string? CustomerProvince { get; set; }
    [MaxLength(100)] public string? CustomerDistrict { get; set; }
    [MaxLength(20)] public string? ProvinceCode { get; set; }
    [MaxLength(100)] public string? ProvinceName { get; set; }
    [MaxLength(20)] public string? WardCode { get; set; }
    [MaxLength(100)] public string? WardName { get; set; }
    [MaxLength(250)] public string? AddressDetail { get; set; }
    [MaxLength(250)] public string? FullAddress { get; set; }
    [MaxLength(500)] public string? Note { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public double ShippingDistanceKm { get; set; }
    public int ShippingDurationMinutes { get; set; }
    public decimal ShippingFee { get; set; }
    [MaxLength(100)] public string? ShippingProvider { get; set; }
    [MaxLength(300)] public string? ShippingFormulaSnapshot { get; set; }
    [MaxLength(50)] public string? VoucherCode { get; set; }
    public DateTime? PaymentExpireAt { get; set; }
    [MaxLength(500)] public string? PaymentUrl { get; set; }
    [MaxLength(100)] public string? PaymentTransactionId { get; set; }
    public ICollection<OrderDetail> Details { get; set; } = new List<OrderDetail>();
}

public class OrderDetail : BaseEntity
{
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    [MaxLength(200)] public string ProductName { get; set; } = string.Empty;
    [MaxLength(250)] public string ProductImage { get; set; } = string.Empty;
    [MaxLength(50)] public string? Warranty { get; set; }
    public int WarrantyMonths { get; set; } = 12;
    public decimal TotalPrice { get; set; }
}

public class ProductImage : BaseEntity
{
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    [MaxLength(1000)] public string ImageUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
}

public class Warranty : BaseEntity
{
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    [MaxLength(100)] public string Coverage { get; set; } = string.Empty;
}

public class WarrantyRequest : BaseEntity
{
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    public int? OrderDetailId { get; set; }
    public OrderDetail? OrderDetail { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    [MaxLength(120)] public string CustomerName { get; set; } = string.Empty;
    [MaxLength(20)] public string Phone { get; set; } = string.Empty;
    [MaxLength(120)] public string? Email { get; set; }
    [MaxLength(200)] public string ProductName { get; set; } = string.Empty;
    [MaxLength(40)] public string RequestCode { get; set; } = string.Empty;
    [MaxLength(80)] public string WarrantyCode { get; set; } = string.Empty;
    [MaxLength(100)] public string? SerialNumber { get; set; }
    public DateTime PurchaseDate { get; set; }
    public int WarrantyMonths { get; set; } = 12;
    [MaxLength(200)] public string IssueTitle { get; set; } = string.Empty;
    [MaxLength(2000)] public string IssueDescription { get; set; } = string.Empty;
    [MaxLength(1000)] public string? EvidencePath { get; set; }
    [MaxLength(50)] public string Status { get; set; } = "Chờ tiếp nhận";
    [MaxLength(2000)] public string? AdminNote { get; set; }
}

public class BuildPcConfig : BaseEntity
{
    public int UserId { get; set; }
    public User? User { get; set; }
    [MaxLength(120)] public string Name { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
}

public class BuildPcItem : BaseEntity
{
    public int BuildPcConfigId { get; set; }
    public BuildPcConfig? BuildPcConfig { get; set; }
    [MaxLength(50)] public string ComponentType { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public Product? Product { get; set; }
}

public class Article : BaseEntity
{
    [MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(200)] public string Slug { get; set; } = string.Empty;
    [MaxLength(50)] public string Type { get; set; } = "Tin công nghệ";
    public string Content { get; set; } = string.Empty;
}



public class ShippingConfig : BaseEntity
{
    public decimal BaseDistanceKm { get; set; } = 3m;
    public decimal BaseFee { get; set; } = 15000m;
    public decimal ExtraFeePerKm { get; set; } = 5000m;
    public decimal MaxDistanceKm { get; set; } = 15m;
    public decimal FreeShippingDistanceKm { get; set; } = 0m;
    public bool IsActive { get; set; } = true;
}

public class ShopLocation : BaseEntity
{
    [MaxLength(120)] public string ShopName { get; set; } = string.Empty;
    [MaxLength(250)] public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsDefault { get; set; } = true;
}

public class SiteSetting : BaseEntity
{
    [MaxLength(120)] public string SiteName { get; set; } = "KKSHOP";
    [MaxLength(1000)] public string? LogoUrl { get; set; }
    [MaxLength(1000)] public string? DealSectionBackgroundUrl { get; set; }
    [MaxLength(1000)] public string? HotPromotionBackgroundUrl { get; set; }
}

public class Feedback : BaseEntity
{
    [MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(120)] public string Email { get; set; } = string.Empty;
    [MaxLength(300)] public string Message { get; set; } = string.Empty;
    public bool IsProcessed { get; set; }
}
