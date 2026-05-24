using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
public class AdminProductsController : Controller
{
    private readonly ApplicationDbContext _db;

    public AdminProductsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, int? categoryId)
    {
        var query = _db.Products.Include(p => p.Category).Include(p => p.ProductImages).AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Name.Contains(keyword));
        if (categoryId.HasValue) query = query.Where(x => x.CategoryId == categoryId);
        ViewBag.Keyword = keyword;
        ViewBag.CategoryId = categoryId;
        ViewBag.Categories = await _db.Categories.OrderBy(x => x.Name).ToListAsync();
        return View(await query.OrderByDescending(p => p.CreatedAt).ToListAsync());
    }

    [HttpGet] public async Task<IActionResult> Create() => View(await BuildUpsertVmAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminProductUpsertVm vm)
    {
        await PopulateCategoriesAsync(vm);
        if (!TryValidateProductImageUrls(vm, true)) return View(vm);

        var product = new Product
        {
            Name = vm.Name,
            ProductCode = $"SP-{Guid.NewGuid():N}"[..16],
            Brand = "N/A",
            Price = vm.Price,
            DiscountPrice = vm.DiscountPrice,
            SalePrice = vm.DiscountPrice,
            IsHotSale = vm.IsHotSale,
            IsDailyDeal = vm.IsDailyDeal,
            IsPromotion = vm.IsPromotion,
            PromotionStartDate = vm.PromotionStartDate,
            PromotionEndDate = vm.PromotionEndDate,
            StockQuantity = vm.StockQuantity,
            WarrantyMonths = vm.WarrantyMonths,
            WarrantyDuration = $"{vm.WarrantyMonths} tháng",
            CategoryId = vm.CategoryId,
            ShortDescription = BuildShortDescription(vm.Description),
            Description = vm.Description,
            DetailDescription = vm.Description,
            Specifications = vm.Specifications,
            ComponentType = "Khác",
            IsActive = vm.IsActive,
            IsInStock = vm.StockQuantity > 0,
            Slug = BuildSlug(vm.Name),
            ThumbnailImage = vm.ThumbnailImageUrl.Trim()
        };

        AddProductImagesFromUrls(product, vm.ProductImageUrlsText);
        EnsurePrimaryImage(product);

        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Đã thêm sản phẩm thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vm = await BuildUpsertVmAsync(id);
        return vm == null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminProductUpsertVm vm)
    {
        await PopulateCategoriesAsync(vm);
        if (!TryValidateProductImageUrls(vm, false))
        {
            await PopulateExistingImagesAsync(vm);
            return View(vm);
        }

        var product = await _db.Products.Include(p => p.ProductImages).FirstOrDefaultAsync(x => x.Id == vm.Id);
        if (product == null) return NotFound();

        product.Name = vm.Name;
        product.Price = vm.Price;
        product.DiscountPrice = vm.DiscountPrice;
        product.SalePrice = vm.DiscountPrice;
        product.IsHotSale = vm.IsHotSale;
        product.IsDailyDeal = vm.IsDailyDeal;
        product.IsPromotion = vm.IsPromotion;
        product.PromotionStartDate = vm.PromotionStartDate;
        product.PromotionEndDate = vm.PromotionEndDate;
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
            foreach (var image in imagesToDelete) _db.ProductImages.Remove(image);
        }

        ApplyExistingImageOrder(product, vm.ExistingImageOrder);
        if (!string.IsNullOrWhiteSpace(vm.ThumbnailImageUrl)) product.ThumbnailImage = vm.ThumbnailImageUrl.Trim();
        AddProductImagesFromUrls(product, vm.ProductImageUrlsText);
        EnsurePrimaryImage(product);

        await _db.SaveChangesAsync();
        TempData["Ok"] = "Đã cập nhật sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.Include(p => p.ProductImages).FirstOrDefaultAsync(x => x.Id == id);
        if (product == null) return RedirectToAction(nameof(Index));
        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Đã xóa sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<AdminProductUpsertVm?> BuildUpsertVmAsync(int? productId = null) { /* omitted for brevity */
        var vm = new AdminProductUpsertVm(); await PopulateCategoriesAsync(vm); if (!productId.HasValue) return vm;
        var product = await _db.Products.Include(p => p.ProductImages).FirstOrDefaultAsync(x => x.Id == productId.Value); if (product == null) return null;
        vm.Id = product.Id; vm.Name = product.Name; vm.Price = product.Price; vm.DiscountPrice = product.DiscountPrice ?? product.SalePrice;
        vm.IsHotSale = product.IsHotSale; vm.IsDailyDeal = product.IsDailyDeal; vm.IsPromotion = product.IsPromotion;
        vm.PromotionStartDate = product.PromotionStartDate; vm.PromotionEndDate = product.PromotionEndDate;
        vm.StockQuantity = product.StockQuantity; vm.WarrantyMonths = product.WarrantyMonths > 0 ? product.WarrantyMonths : 12; vm.CategoryId = product.CategoryId;
        vm.Description = string.IsNullOrWhiteSpace(product.Description) ? product.DetailDescription : product.Description; vm.Specifications = product.Specifications; vm.IsActive = product.IsActive;
        vm.ThumbnailImageUrl = product.ThumbnailImage;
        var orderedImages = product.ProductImages.OrderBy(x => x.SortOrder).ToList(); vm.ExistingImageOrder = orderedImages.Select(x => x.Id).ToList();
        vm.ExistingImages = orderedImages.Select(x => new ProductImageItemVm { Id = x.Id, ImageUrl = x.ImageUrl, IsPrimary = x.IsPrimary, SortOrder = x.SortOrder }).ToList(); return vm; }

    private async Task PopulateCategoriesAsync(AdminProductUpsertVm vm) => vm.Categories = await _db.Categories.OrderBy(x => x.Name).ToListAsync();
    private async Task PopulateExistingImagesAsync(AdminProductUpsertVm vm) { vm.ExistingImages = await _db.ProductImages.Where(x => x.ProductId == vm.Id).OrderBy(x => x.SortOrder).Select(x => new ProductImageItemVm { Id = x.Id, ImageUrl = x.ImageUrl, IsPrimary = x.IsPrimary, SortOrder = x.SortOrder }).ToListAsync(); if (!vm.ExistingImageOrder.Any()) vm.ExistingImageOrder = vm.ExistingImages.Select(x => x.Id).ToList(); }

    private bool TryValidateProductImageUrls(AdminProductUpsertVm vm, bool requireThumbnail)
    {
        if (requireThumbnail && string.IsNullOrWhiteSpace(vm.ThumbnailImageUrl)) ModelState.AddModelError(nameof(vm.ThumbnailImageUrl), "Vui lòng nhập URL thumbnail ảnh.");
        ValidateUrl(vm.ThumbnailImageUrl, nameof(vm.ThumbnailImageUrl));
        foreach (var url in SplitImageUrls(vm.ProductImageUrlsText)) ValidateUrl(url, nameof(vm.ProductImageUrlsText));
        return ModelState.IsValid;
    }

    private void AddProductImagesFromUrls(Product product, string urlsText)
    {
        var sort = product.ProductImages.Count == 0 ? 1 : product.ProductImages.Max(x => x.SortOrder) + 1;
        foreach (var url in SplitImageUrls(urlsText)) product.ProductImages.Add(new ProductImage { ImageUrl = url, SortOrder = sort++, IsPrimary = false });
    }

    private static List<string> SplitImageUrls(string urlsText) => (urlsText ?? string.Empty).Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    private void ValidateUrl(string? value, string key) { if (string.IsNullOrWhiteSpace(value)) return; if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) ModelState.AddModelError(key, "Chỉ chấp nhận URL http/https hợp lệ."); }
    private static void ApplyExistingImageOrder(Product product, List<int> orderedImageIds) { if (!orderedImageIds.Any()) return; var current = product.ProductImages.ToDictionary(x => x.Id); var sort = 1; foreach (var imageId in orderedImageIds) if (current.TryGetValue(imageId, out var image)) image.SortOrder = sort++; foreach (var image in product.ProductImages.Where(x => x.SortOrder <= 0).OrderBy(x => x.Id)) image.SortOrder = sort++; }
    private static void EnsurePrimaryImage(Product product) { var orderedImages = product.ProductImages.OrderBy(x => x.SortOrder).ToList(); if (!orderedImages.Any()) return; var primaryImage = orderedImages.First(); foreach (var image in orderedImages) image.IsPrimary = image.Id == primaryImage.Id; if (string.IsNullOrWhiteSpace(product.ThumbnailImage)) product.ThumbnailImage = primaryImage.ImageUrl; }
    private static string BuildSlug(string input) => string.IsNullOrWhiteSpace(input) ? Guid.NewGuid().ToString("N") : string.Join('-', input.ToLowerInvariant().Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
    private static string BuildShortDescription(string description) => string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? string.Empty;
}
