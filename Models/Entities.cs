using System.ComponentModel.DataAnnotations;

namespace Datn.PcStore.Models;

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
    [MaxLength(200)] public string PasswordHash { get; set; } = string.Empty;
    [MaxLength(20)] public string Phone { get; set; } = string.Empty;
    [MaxLength(250)] public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int RoleId { get; set; }
    public Role? Role { get; set; }
}

public class Category : BaseEntity
{
    [MaxLength(120)] public string Name { get; set; } = string.Empty;
    [MaxLength(50)] public string IconClass { get; set; } = "bi bi-grid";
    public int? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
}

public class Product : BaseEntity
{
    [MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(220)] public string Slug { get; set; } = string.Empty;
    [MaxLength(50)] public string ProductCode { get; set; } = string.Empty;
    [MaxLength(80)] public string Brand { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public decimal? SalePrice { get; set; }
    public int StockQuantity { get; set; }
    [MaxLength(1000)] public string ThumbnailImage { get; set; } = string.Empty;
    [MaxLength(500)] public string ShortDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DetailDescription { get; set; } = string.Empty;
    public string Specifications { get; set; } = string.Empty;
    public int WarrantyMonths { get; set; } = 12;
    [MaxLength(50)] public string WarrantyDuration { get; set; } = "12 tháng";
    public bool IsActive { get; set; } = true;
    public bool IsInStock { get; set; } = true;
    public bool HasSoftwareLicense { get; set; }
    [MaxLength(40)] public string ComponentType { get; set; } = "Khác";
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

public enum OrderStatus { Pending = 1, Processing, Delivering, Completed, Cancelled }

public class Order : BaseEntity
{
    public int? UserId { get; set; }
    public User? User { get; set; }
    [MaxLength(120)] public string ReceiverName { get; set; } = string.Empty;
    [MaxLength(20)] public string ReceiverPhone { get; set; } = string.Empty;
    [MaxLength(250)] public string ShippingAddress { get; set; } = string.Empty;
    [MaxLength(30)] public string PaymentMethod { get; set; } = "COD";
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
    public int UserId { get; set; }
    public User? User { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    [MaxLength(200)] public string IssueDescription { get; set; } = string.Empty;
    [MaxLength(50)] public string Status { get; set; } = "Mới tạo";
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
