using Datn.PcStore.Models;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Data;

public static class SeedData
{
    public static async Task InitializeAsync(ApplicationDbContext db)
    {
        var roleNames = new[] { "Admin", "Staff", "SupportStaff", "CustomerSupport", "Customer" };
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

        var requiredCategories = new[]
        {
            new Category { Name = "PC Gaming", IconClass = "bi bi-pc-display" },
            new Category { Name = "AMD Gaming", IconClass = "bi bi-gpu-card" },
            new Category { Name = "Workstation", IconClass = "bi bi-cpu" },
            new Category { Name = "PC Mini", IconClass = "bi bi-device-ssd" },
            new Category { Name = "PC Văn Phòng", IconClass = "bi bi-briefcase" },
            new Category { Name = "Linh kiện", IconClass = "bi bi-memory" },
            new Category { Name = "Laptop", IconClass = "bi bi-laptop" },
            new Category { Name = "Màn hình", IconClass = "bi bi-display" }
        };

        foreach (var category in requiredCategories)
        {
            if (!await db.Categories.AnyAsync(c => c.Name == category.Name))
            {
                db.Categories.Add(category);
            }
        }

        await db.SaveChangesAsync();

        if (!await db.Banners.AnyAsync())
        {
            var laptopCategoryId = await db.Categories.Where(c => c.Name == "Laptop").Select(c => c.Id).FirstAsync();
            db.Banners.AddRange(
                new Banner
                {
                    Title = "Flash Sale Gaming Gear",
                    Description = "Giảm sâu dàn PC gaming, số lượng có hạn",
                    ImageUrl = "https://res.cloudinary.com/demo/image/upload/v1312461204/sample.jpg",
                    LinkUrl = "/Products",
                    SortOrder = 1,
                    IsActive = true,
                    Position = "MainBanner"
                },
                new Banner
                {
                    Title = "Laptop cho sinh viên",
                    Description = "Nhiều lựa chọn từ học tập đến thiết kế",
                    ImageUrl = "https://res.cloudinary.com/demo/image/upload/v1312461204/flower.jpg",
                    LinkUrl = $"/Products?CategoryId={laptopCategoryId}",
                    SortOrder = 2,
                    IsActive = true,
                    Position = "SubBanner"
                },
                new Banner
                {
                    Title = "Góc học tập hiệu quả",
                    Description = "Màn hình và phụ kiện cho học tập",
                    ImageUrl = "https://res.cloudinary.com/demo/image/upload/v1312461204/animals.jpg",
                    LinkUrl = "/Products",
                    SortOrder = 3,
                    IsActive = true,
                    Position = "SubBanner"
                },
                new Banner
                {
                    Title = "Linh kiện nâng cấp",
                    Description = "SSD, RAM, tản nhiệt chính hãng",
                    ImageUrl = "https://res.cloudinary.com/demo/image/upload/v1312461204/couple.jpg",
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
            var categoryMap = await db.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);

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
                    CategoryId = categoryMap["PC Gaming"],
                    ThumbnailImage = "https://res.cloudinary.com/demo/image/upload/v1312461204/sample.jpg",
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
                        new() { ImageUrl = "https://res.cloudinary.com/demo/image/upload/v1312461204/sample.jpg", SortOrder = 1, IsPrimary = true },
                        new() { ImageUrl = "https://res.cloudinary.com/demo/image/upload/v1312461204/flower.jpg", SortOrder = 2, IsPrimary = false }
                    }
                },
                new()
                {
                    Name = "PC AMD R7 7800X3D RTX 4070",
                    Slug = "pc-amd-r7-7800x3d-rtx4070",
                    ProductCode = "AMDG1",
                    Brand = "KKSHOP",
                    Price = 34990000,
                    StockQuantity = 4,
                    CategoryId = categoryMap["AMD Gaming"],
                    ThumbnailImage = "https://res.cloudinary.com/demo/image/upload/v1312461204/flower.jpg",
                    ShortDescription = "PC gaming CPU AMD hiệu năng cao.",
                    Description = "Tối ưu FPS cho game AAA và eSports.",
                    DetailDescription = "Ryzen 7 7800X3D kết hợp RTX 4070.",
                    Specifications = "Ryzen 7 7800X3D | RAM 32GB DDR5 | SSD 1TB",
                    WarrantyMonths = 36,
                    WarrantyDuration = "36 tháng",
                    ComponentType = "PC",
                    IsInStock = true,
                    IsActive = true
                },
                new()
                {
                    Name = "Workstation W5 RTX 4080",
                    Slug = "workstation-w5-rtx4080",
                    ProductCode = "WORK5",
                    Brand = "KKSHOP",
                    Price = 52990000,
                    StockQuantity = 3,
                    CategoryId = categoryMap["Workstation"],
                    ThumbnailImage = "https://res.cloudinary.com/demo/image/upload/v1312461204/couple.jpg",
                    ShortDescription = "Máy trạm cho render, CAD, AI.",
                    Description = "Đáp ứng workload dựng hình và machine learning.",
                    DetailDescription = "CPU đa nhân + RTX 4080 + RAM lớn.",
                    Specifications = "Core i9 | RAM 64GB | RTX 4080 | SSD 2TB",
                    WarrantyMonths = 36,
                    WarrantyDuration = "36 tháng",
                    ComponentType = "PC",
                    IsInStock = true,
                    IsActive = true
                },
                new()
                {
                    Name = "PC Mini M2",
                    Slug = "pc-mini-m2",
                    ProductCode = "MINI2",
                    Brand = "KKSHOP",
                    Price = 12990000,
                    StockQuantity = 7,
                    CategoryId = categoryMap["PC Mini"],
                    ThumbnailImage = "https://res.cloudinary.com/demo/image/upload/v1312461204/animals.jpg",
                    ShortDescription = "PC mini nhỏ gọn tiết kiệm diện tích.",
                    Description = "Phù hợp bàn làm việc nhỏ và quầy dịch vụ.",
                    DetailDescription = "Thiết kế mini-ITX yên tĩnh, dễ bố trí.",
                    Specifications = "Core i5 | RAM 16GB | SSD 512GB",
                    WarrantyMonths = 24,
                    WarrantyDuration = "24 tháng",
                    ComponentType = "PC",
                    IsInStock = true,
                    IsActive = true
                },
                new()
                {
                    Name = "CPU AMD Ryzen 5 7600",
                    Slug = "cpu-amd-ryzen-5-7600",
                    ProductCode = "CPU7600",
                    Brand = "AMD",
                    Price = 5490000,
                    StockQuantity = 20,
                    CategoryId = categoryMap["Linh kiện"],
                    ThumbnailImage = "https://res.cloudinary.com/demo/image/upload/v1312461204/sample.jpg",
                    ShortDescription = "CPU AMD thế hệ mới cho gaming.",
                    Description = "6 nhân 12 luồng, socket AM5.",
                    DetailDescription = "Phù hợp build gaming/office cân bằng.",
                    Specifications = "6C/12T | Boost 5.1GHz | AM5",
                    WarrantyMonths = 36,
                    WarrantyDuration = "36 tháng",
                    ComponentType = "CPU",
                    IsInStock = true,
                    IsActive = true
                },
                new()
                {
                    Name = "Laptop Swift 14",
                    Slug = "laptop-swift-14",
                    ProductCode = "LAP14",
                    Brand = "Aster",
                    Price = 18990000,
                    StockQuantity = 8,
                    CategoryId = categoryMap["PC Văn Phòng"],
                    ThumbnailImage = "https://res.cloudinary.com/demo/image/upload/v1312461204/animals.jpg",
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
                        new() { ImageUrl = "https://res.cloudinary.com/demo/image/upload/v1312461204/animals.jpg", SortOrder = 1, IsPrimary = true },
                        new() { ImageUrl = "https://res.cloudinary.com/demo/image/upload/v1312461204/couple.jpg", SortOrder = 2, IsPrimary = false }
                    }
                }
            };

            db.Products.AddRange(products);
            await db.SaveChangesAsync();
        }

        var cpuCount = await db.Products.CountAsync(p => p.IsActive && (
            (p.ComponentType ?? string.Empty).ToLower().Contains("cpu")
            || (p.Name ?? string.Empty).ToLower().Contains("cpu")
            || (p.Name ?? string.Empty).ToLower().Contains("intel")
            || (p.Name ?? string.Empty).ToLower().Contains("ryzen")
            || (p.Name ?? string.Empty).ToLower().Contains("core i3")
            || (p.Name ?? string.Empty).ToLower().Contains("core i5")
            || (p.Name ?? string.Empty).ToLower().Contains("core i7")
            || (p.Name ?? string.Empty).ToLower().Contains("core i9")
        ));

        if (cpuCount < 5)
        {
            var categoryMap = await db.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);
            var linhKienCategoryId = categoryMap["Linh kiện"];
            var cpuProducts = new List<Product>
            {
                new() { Name = "CPU Intel Core i3-14100F", Slug = "cpu-intel-core-i3-14100f", ProductCode = "CPU-I3-14100F", Brand = "Intel", Price = 2990000, StockQuantity = 12, CategoryId = linhKienCategoryId, ThumbnailImage = "https://res.cloudinary.com/demo/image/upload/v1312461204/sample.jpg", ShortDescription = "CPU Intel phổ thông", Description = "4 nhân 8 luồng", DetailDescription = "Phù hợp gaming/esport", Specifications = "Core i3-14100F", WarrantyMonths = 36, WarrantyDuration = "36 tháng", ComponentType = "CPU", IsInStock = true, IsActive = true },
                new() { Name = "CPU Intel Core i5-14600K", Slug = "cpu-intel-core-i5-14600k", ProductCode = "CPU-I5-14600K", Brand = "Intel", Price = 7890000, StockQuantity = 9, CategoryId = linhKienCategoryId, ThumbnailImage = "https://res.cloudinary.com/demo/image/upload/v1312461204/flower.jpg", ShortDescription = "CPU Intel tầm trung", Description = "Hiệu năng cao cho game", DetailDescription = "Core i5 thế hệ 14", Specifications = "Core i5-14600K", WarrantyMonths = 36, WarrantyDuration = "36 tháng", ComponentType = "CPU", IsInStock = true, IsActive = true },
                new() { Name = "CPU Intel Core i7-14700K", Slug = "cpu-intel-core-i7-14700k", ProductCode = "CPU-I7-14700K", Brand = "Intel", Price = 10990000, StockQuantity = 7, CategoryId = linhKienCategoryId, ThumbnailImage = "https://res.cloudinary.com/demo/image/upload/v1312461204/animals.jpg", ShortDescription = "CPU Intel cao cấp", Description = "Tối ưu đa nhiệm", DetailDescription = "Core i7 thế hệ 14", Specifications = "Core i7-14700K", WarrantyMonths = 36, WarrantyDuration = "36 tháng", ComponentType = "CPU", IsInStock = true, IsActive = true },
                new() { Name = "CPU AMD Ryzen 5 7500F", Slug = "cpu-amd-ryzen-5-7500f", ProductCode = "CPU-R5-7500F", Brand = "AMD", Price = 4190000, StockQuantity = 15, CategoryId = linhKienCategoryId, ThumbnailImage = "https://res.cloudinary.com/demo/image/upload/v1312461204/couple.jpg", ShortDescription = "CPU AMD gaming", Description = "6 nhân 12 luồng", DetailDescription = "Giá tốt cho AM5", Specifications = "Ryzen 5 7500F", WarrantyMonths = 36, WarrantyDuration = "36 tháng", ComponentType = "CPU", IsInStock = true, IsActive = true },
                new() { Name = "CPU AMD Ryzen 7 7800X3D", Slug = "cpu-amd-ryzen-7-7800x3d", ProductCode = "CPU-R7-7800X3D", Brand = "AMD", Price = 9990000, StockQuantity = 6, CategoryId = linhKienCategoryId, ThumbnailImage = "https://res.cloudinary.com/demo/image/upload/v1312461204/sample.jpg", ShortDescription = "CPU AMD cao cấp", Description = "Tối ưu gaming 3D V-Cache", DetailDescription = "Hiệu năng game hàng đầu", Specifications = "Ryzen 7 7800X3D", WarrantyMonths = 36, WarrantyDuration = "36 tháng", ComponentType = "CPU", IsInStock = true, IsActive = true }
            };

            var existingSlugs = (await db.Products.Select(p => p.Slug).ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var productsToInsert = cpuProducts.Where(p => !existingSlugs.Contains(p.Slug)).ToList();
            if (productsToInsert.Count > 0)
            {
                db.Products.AddRange(productsToInsert);
                await db.SaveChangesAsync();
            }
        }


        var accessoryTypes = new[] { "Monitor", "Keyboard", "Mouse", "Headphone" };
        if (!await db.Products.AnyAsync(p => accessoryTypes.Contains(p.ComponentType)))
        {
            var categoryMap = await db.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);
            var accessoryCategoryId = categoryMap.TryGetValue("Linh kiện", out var lkId) ? lkId : categoryMap.Values.First();
            db.Products.AddRange(
                new Product { Name = "Màn hình ASUS TUF Gaming VG249Q3A 24 inch 180Hz", Slug = "man-hinh-asus-tuf-vg249q3a", ProductCode = "MON-ASUS-VG249Q3A", Brand = "ASUS", Price = 3990000, DiscountPrice = 3590000, SalePrice = 3590000, StockQuantity = 10, CategoryId = accessoryCategoryId, ThumbnailImage = "https://ttgshop.vn/media/category/cat_big_1003486567.png", ShortDescription = "Màn hình gaming FHD 180Hz", Description = "Màn hình IPS 24 inch, Full HD, 180Hz.", DetailDescription = "Màn hình IPS 24 inch, Full HD, 180Hz.", Specifications = "24 inch | FHD | 180Hz | IPS", WarrantyMonths = 36, WarrantyDuration = "36 tháng", ComponentType = "Monitor", IsInStock = true, IsActive = true },
                new Product { Name = "Bàn phím cơ AKKO 3087 RGB", Slug = "ban-phim-co-akko-3087-rgb", ProductCode = "KEY-AKKO-3087", Brand = "AKKO", Price = 1490000, DiscountPrice = 1290000, SalePrice = 1290000, StockQuantity = 15, CategoryId = accessoryCategoryId, ThumbnailImage = "https://ttgshop.vn/media/category/cat_big_1003353341.png", ShortDescription = "Bàn phím cơ TKL RGB", Description = "Layout TKL, switch cơ, RGB.", DetailDescription = "Layout TKL, switch cơ, RGB.", Specifications = "TKL | Mechanical | USB-C | RGB", WarrantyMonths = 12, WarrantyDuration = "12 tháng", ComponentType = "Keyboard", IsInStock = true, IsActive = true },
                new Product { Name = "Chuột Logitech G304 Lightspeed", Slug = "chuot-logitech-g304-lightspeed", ProductCode = "MOU-LOGI-G304", Brand = "Logitech", Price = 990000, DiscountPrice = 790000, SalePrice = 790000, StockQuantity = 20, CategoryId = accessoryCategoryId, ThumbnailImage = "https://ttgshop.vn/media/category/cat_big_1003353370.png", ShortDescription = "Chuột gaming không dây", Description = "Kết nối Lightspeed, cảm biến HERO.", DetailDescription = "Kết nối Lightspeed, cảm biến HERO.", Specifications = "Wireless | HERO | 12000 DPI", WarrantyMonths = 24, WarrantyDuration = "24 tháng", ComponentType = "Mouse", IsInStock = true, IsActive = true },
                new Product { Name = "Tai nghe HyperX Cloud Stinger 2", Slug = "tai-nghe-hyperx-cloud-stinger-2", ProductCode = "HP-HYPERX-STINGER2", Brand = "HyperX", Price = 1290000, DiscountPrice = 1090000, SalePrice = 1090000, StockQuantity = 12, CategoryId = accessoryCategoryId, ThumbnailImage = "https://ttgshop.vn/media/category/cat_big_1004013226.jpg", ShortDescription = "Tai nghe gaming âm thanh vòm", Description = "Micro chống ồn, đệm tai êm.", DetailDescription = "Micro chống ồn, đệm tai êm.", Specifications = "3.5mm | Stereo | Mic chống ồn", WarrantyMonths = 24, WarrantyDuration = "24 tháng", ComponentType = "Headphone", IsInStock = true, IsActive = true }
            );
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
