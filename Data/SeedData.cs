using Datn.PcStore.Models;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Data;

public static class SeedData
{
    public static async Task InitializeAsync(ApplicationDbContext db)
    {
        var roleNames = new[] { "Admin", "Staff", "Customer" };
        foreach (var roleName in roleNames)
        {
            if (!await db.Roles.AnyAsync(r => r.Name == roleName))
            {
                db.Roles.Add(new Role { Name = roleName });
            }
        }
        await db.SaveChangesAsync();

        var adminRoleId = await db.Roles.Where(r => r.Name == "Admin").Select(r => r.Id).FirstAsync();
        if (!await db.Users.AnyAsync(u => u.Email == "admin@pcstore.local"))
        {
            var authService = new Services.AuthService(db);
            db.Users.Add(new User
            {
                Username = "admin",
                FullName = "System Admin",
                Email = "admin@pcstore.local",
                PasswordHash = authService.HashPassword("123456"),
                Phone = "0900000000",
                Address = "Hà Nội",
                RoleId = adminRoleId,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        if (!await db.Categories.AnyAsync())
        {
            db.Categories.AddRange(
                new Category { Name = "PC Gaming", IconClass = "bi bi-pc-display" },
                new Category { Name = "Laptop", IconClass = "bi bi-laptop" },
                new Category { Name = "Màn hình", IconClass = "bi bi-display" },
                new Category { Name = "Linh kiện", IconClass = "bi bi-cpu" }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Banners.AnyAsync())
        {
            var laptopCategoryId = await db.Categories.Where(c => c.Name == "Laptop").Select(c => c.Id).FirstAsync();
            db.Banners.AddRange(
                new Banner
                {
                    Title = "Flash Sale Gaming Gear",
                    Description = "Giảm sâu dàn PC gaming, số lượng có hạn",
                    ImageUrl = "/images/banners/home-main-banner.jpg",
                    LinkUrl = "/Products",
                    SortOrder = 1,
                    IsActive = true,
                    Position = "MainBanner"
                },
                new Banner
                {
                    Title = "Laptop cho sinh viên",
                    Description = "Nhiều lựa chọn từ học tập đến thiết kế",
                    ImageUrl = "/images/banners/home-sub-1.jpg",
                    LinkUrl = $"/Products?CategoryId={laptopCategoryId}",
                    SortOrder = 2,
                    IsActive = true,
                    Position = "SubBanner"
                },
                new Banner
                {
                    Title = "Góc học tập hiệu quả",
                    Description = "Màn hình và phụ kiện cho học tập",
                    ImageUrl = "/images/banners/home-sub-2.jpg",
                    LinkUrl = "/Products",
                    SortOrder = 3,
                    IsActive = true,
                    Position = "SubBanner"
                },
                new Banner
                {
                    Title = "Linh kiện nâng cấp",
                    Description = "SSD, RAM, tản nhiệt chính hãng",
                    ImageUrl = "/images/banners/home-sub-3.jpg",
                    LinkUrl = "/Products",
                    SortOrder = 4,
                    IsActive = true,
                    Position = "SubBanner"
                }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Products.AnyAsync())
        {
            var pcCategoryId = await db.Categories.Where(c => c.Name == "PC Gaming").Select(c => c.Id).FirstAsync();
            var laptopCategoryId = await db.Categories.Where(c => c.Name == "Laptop").Select(c => c.Id).FirstAsync();

            var products = new List<Product>
            {
                new()
                {
                    Name = "PC KKSHOP G5",
                    Slug = "pc-kkshop-g5",
                    ProductCode = "PCG5",
                    Brand = "KKSHOP",
                    Price = 25990000,
                    DiscountPrice = 23990000,
                    SalePrice =23990000,
                    StockQuantity = 5,
                    CategoryId = pcCategoryId,
                    ThumbnailImage = "/images/products/product-1.jpg",
                    ShortDescription = "PC gaming tầm trung, chiến game 2K.",
                    Description = "Cấu hình gồm i5 + RTX 4060.",
                    DetailDescription = "Cấu hình gồm i5 + RTX 4060.",
                    Specifications = "CPU i5-13400F | RAM 16GB DDR4 | SSD 1TB",
                    WarrantyMonths = 24,
                    WarrantyDuration = "24 tháng",
                    ComponentType = "PC",
                    IsInStock = true,
                    IsActive = true,
                    ProductImages = new List<ProductImage>
                    {
                        new() { ImageUrl = "/images/products/product-1.jpg", SortOrder = 1, IsPrimary = true },
                        new() { ImageUrl = "/images/products/product-2.jpg", SortOrder = 2, IsPrimary = false }
                    }
                },
                new()
                {
                    Name = "Laptop Swift 14",
                    Slug = "laptop-swift-14",
                    ProductCode = "LAP14",
                    Brand = "Aster",
                    Price = 18990000,
                    StockQuantity = 8,
                    CategoryId = laptopCategoryId,
                    ThumbnailImage = "/images/products/product-3.jpg",
                    ShortDescription = "Laptop văn phòng mỏng nhẹ.",
                    Description = "Phù hợp sinh viên và dân công sở.",
                    DetailDescription = "Phù hợp sinh viên và dân công sở.",
                    Specifications = "CPU i5 | RAM 16GB | SSD 512GB",
                    WarrantyMonths = 12,
                    WarrantyDuration = "12 tháng",
                    ComponentType = "Laptop",
                    IsInStock = true,
                    IsActive = true,
                    ProductImages = new List<ProductImage>
                    {
                        new() { ImageUrl = "/images/products/product-3.jpg", SortOrder = 1, IsPrimary = true },
                        new() { ImageUrl = "/images/products/product-4.jpg", SortOrder = 2, IsPrimary = false }
                    }
                }
            };

            db.Products.AddRange(products);
            await db.SaveChangesAsync();
        }

        if (!await db.SiteSettings.AnyAsync())
        {
            db.SiteSettings.Add(new SiteSetting
            {
                SiteName = "KKSHOP",
                LogoUrl = null,
                DealSectionBackgroundUrl = null,
                HotPromotionBackgroundUrl = null
            });
            await db.SaveChangesAsync();
        }

        if (!await db.Articles.AnyAsync())
        {
            db.Articles.AddRange(
                new Article { Title = "Top 5 cấu hình gaming đáng mua 2026", Slug = "top-5-cau-hinh-gaming-2026", Type = "Tin tức", Content = "Gợi ý cấu hình theo ngân sách từ 15 đến 40 triệu." },
                new Article { Title = "Ưu đãi tháng 4 cho laptop", Slug = "uu-dai-thang-4-laptop", Type = "Khuyến mãi", Content = "Tặng chuột không dây và balo khi mua laptop." }
            );
            await db.SaveChangesAsync();
        }
    }
}
