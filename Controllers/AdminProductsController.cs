using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Datn.PcStore.Services;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
public class AdminProductsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IProductImageStorageService _imageStorage;

    public AdminProductsController(ApplicationDbContext db, IProductImageStorageService imageStorage)
    {
        _db = db;
        _imageStorage = imageStorage;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, int? categoryId)
    {
        var query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.Name.Contains(keyword));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId);
        }

        ViewBag.Keyword = keyword;
        ViewBag.CategoryId = categoryId;
        ViewBag.Categories = await _db.Categories.OrderBy(x => x.Name).ToListAsync();

        return View(await query.OrderByDescending(p => p.CreatedAt).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(await BuildUpsertVmAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminProductUpsertVm vm)
    {
        await PopulateCategoriesAsync(vm);

        if (!ValidateImages(vm.NewImages))
        {
            return View(vm);
        }

        if (vm.NewImages.Count == 0)
        {
            ModelState.AddModelError(nameof(vm.NewImages), "Vui lòng thêm ít nhất 1 ảnh sản phẩm.");
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var product = new Product
        {
            Name = vm.Name,
            ProductCode = $"SP-{Guid.NewGuid():N}"[..16],
            Brand = "N/A",
            Price = vm.Price,
            DiscountPrice = vm.DiscountPrice,
            SalePrice = vm.DiscountPrice,
            StockQuantity = vm.StockQuantity,
            WarrantyMonths = vm.WarrantyMonths,
            WarrantyDuration = $"{vm.WarrantyMonths} tháng",
            CategoryId = vm.CategoryId,
            ShortDescription = BuildShortDescription(vm.Description),
            Description = vm.Description,
            DetailDescription = vm.Description,
            Specifications = vm.Specifications,
            ComponentType = "Khác",
            CpuSocket = null,
            RamType = null,
            IsActive = vm.IsActive,
            IsInStock = vm.StockQuantity > 0,
            Slug = BuildSlug(vm.Name),
            ThumbnailImage = "/images/no-image.png"
        };

        await AddNewImagesAsync(product, vm.NewImages);

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Đã thêm sản phẩm thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vm = await BuildUpsertVmAsync(id);
        if (vm == null)
        {
            return NotFound();
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminProductUpsertVm vm)
    {
        await PopulateCategoriesAsync(vm);

        if (!ValidateImages(vm.NewImages))
        {
            await PopulateExistingImagesAsync(vm);
            return View(vm);
        }

        if (!ModelState.IsValid)
        {
            await PopulateExistingImagesAsync(vm);
            return View(vm);
        }

        var product = await _db.Products
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(x => x.Id == vm.Id);

        if (product == null)
        {
            return NotFound();
        }

        product.Name = vm.Name;
        product.Price = vm.Price;
        product.DiscountPrice = vm.DiscountPrice;
        product.SalePrice = vm.DiscountPrice;
        product.StockQuantity = vm.StockQuantity;
        product.WarrantyMonths = vm.WarrantyMonths;
        product.WarrantyDuration = $"{vm.WarrantyMonths} tháng";
        product.CategoryId = vm.CategoryId;
        product.ShortDescription = BuildShortDescription(vm.Description);
        product.Description = vm.Description;
        product.DetailDescription = vm.Description;
        product.Specifications = vm.Specifications;
        product.IsActive = vm.IsActive;
        product.IsInStock = vm.StockQuantity > 0;
        product.Slug = BuildSlug(vm.Name);
        product.UpdatedAt = DateTime.UtcNow;

        if (vm.RemoveImageIds.Any())
        {
            var imagesToDelete = product.ProductImages.Where(x => vm.RemoveImageIds.Contains(x.Id)).ToList();
            foreach (var image in imagesToDelete)
            {
                _imageStorage.DeleteImage(image.ImageUrl);
                _db.ProductImages.Remove(image);
            }
        }

        // Áp thứ tự ảnh hiện có theo danh sách id từ UI (nút Lên/Xuống).
        ApplyExistingImageOrder(product, vm.ExistingImageOrder);

        // Chỉ thêm ảnh khi admin thực sự upload file mới.
        await AddNewImagesAsync(product, vm.NewImages);

        EnsurePrimaryImage(product);

        await _db.SaveChangesAsync();
        TempData["Ok"] = "Đã cập nhật sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.Include(p => p.ProductImages).FirstOrDefaultAsync(x => x.Id == id);
        if (product == null)
        {
            return RedirectToAction(nameof(Index));
        }

        foreach (var image in product.ProductImages)
        {
            _imageStorage.DeleteImage(image.ImageUrl);
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Đã xóa sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<AdminProductUpsertVm?> BuildUpsertVmAsync(int? productId = null)
    {
        var vm = new AdminProductUpsertVm();
        await PopulateCategoriesAsync(vm);

        if (!productId.HasValue)
        {
            return vm;
        }

        var product = await _db.Products
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(x => x.Id == productId.Value);

        if (product == null)
        {
            return null;
        }

        vm.Id = product.Id;
        vm.Name = product.Name;
        vm.Price = product.Price;
        vm.DiscountPrice = product.DiscountPrice ?? product.SalePrice;
        vm.StockQuantity = product.StockQuantity;
        vm.WarrantyMonths = product.WarrantyMonths > 0 ? product.WarrantyMonths : 12;
        vm.CategoryId = product.CategoryId;
        vm.Description = string.IsNullOrWhiteSpace(product.Description) ? product.DetailDescription : product.Description;
        vm.Specifications = product.Specifications;
        vm.IsActive = product.IsActive;

        var orderedImages = product.ProductImages.OrderBy(x => x.SortOrder).ToList();
        vm.ExistingImageOrder = orderedImages.Select(x => x.Id).ToList();
        vm.ExistingImages = orderedImages.Select(x => new ProductImageItemVm
        {
            Id = x.Id,
            ImageUrl = x.ImageUrl,
            IsPrimary = x.IsPrimary,
            SortOrder = x.SortOrder
        }).ToList();

        return vm;
    }

    private async Task PopulateCategoriesAsync(AdminProductUpsertVm vm)
    {
        vm.Categories = await _db.Categories.OrderBy(x => x.Name).ToListAsync();
    }

    private async Task PopulateExistingImagesAsync(AdminProductUpsertVm vm)
    {
        vm.ExistingImages = await _db.ProductImages
            .Where(x => x.ProductId == vm.Id)
            .OrderBy(x => x.SortOrder)
            .Select(x => new ProductImageItemVm
            {
                Id = x.Id,
                ImageUrl = x.ImageUrl,
                IsPrimary = x.IsPrimary,
                SortOrder = x.SortOrder
            })
            .ToListAsync();

        if (!vm.ExistingImageOrder.Any())
        {
            vm.ExistingImageOrder = vm.ExistingImages.Select(x => x.Id).ToList();
        }
    }

    private bool ValidateImages(List<IFormFile> files)
    {
        foreach (var file in files)
        {
            if (!_imageStorage.IsValidImage(file, out var error))
            {
                ModelState.AddModelError(nameof(AdminProductUpsertVm.NewImages), error);
            }
        }

        return ModelState.IsValid;
    }

    private async Task AddNewImagesAsync(Product product, List<IFormFile> newImages)
    {
        if (newImages.Count == 0)
        {
            return;
        }

        var currentSortOrder = product.ProductImages.Count == 0 ? 1 : product.ProductImages.Max(x => x.SortOrder) + 1;

        foreach (var imageFile in newImages)
        {
            var path = await _imageStorage.SaveImageAsync(imageFile);
            product.ProductImages.Add(new ProductImage
            {
                ImageUrl = path,
                SortOrder = currentSortOrder++,
                IsPrimary = false
            });
        }
    }

    private static void ApplyExistingImageOrder(Product product, List<int> orderedImageIds)
    {
        if (!orderedImageIds.Any())
        {
            return;
        }

        var current = product.ProductImages.ToDictionary(x => x.Id);
        var sort = 1;
        foreach (var imageId in orderedImageIds)
        {
            if (current.TryGetValue(imageId, out var image))
            {
                image.SortOrder = sort++;
            }
        }

        foreach (var image in product.ProductImages.Where(x => x.SortOrder <= 0).OrderBy(x => x.Id))
        {
            image.SortOrder = sort++;
        }
    }

    private static void EnsurePrimaryImage(Product product)
    {
        var orderedImages = product.ProductImages.OrderBy(x => x.SortOrder).ToList();
        if (!orderedImages.Any())
        {
            product.ThumbnailImage = "/images/no-image.png";
            return;
        }

        var primaryImage = orderedImages.First();
        foreach (var image in orderedImages)
        {
            image.IsPrimary = image.Id == primaryImage.Id;
        }

        product.ThumbnailImage = primaryImage.ImageUrl;
    }

    private static string BuildSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Guid.NewGuid().ToString("N");
        }

        var slug = input.ToLowerInvariant().Trim();
        slug = string.Join('-', slug.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        slug = slug.Replace(' ', '-');
        return slug;
    }

    private static string BuildShortDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        var firstLine = description
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        return firstLine ?? string.Empty;
    }
}
