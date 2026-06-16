using Datn.PcStore.Data;
using Datn.PcStore.Helpers;
using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
public class AdminComponentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AdminComponentsController> _logger;

    public AdminComponentsController(ApplicationDbContext db, ILogger<AdminComponentsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, string? componentType, string? brand, decimal? minPrice, decimal? maxPrice, bool? isActive, bool? inStock)
    {
        var query = _db.Products.Include(p => p.Category).Include(p => p.ProductImages)
            .Where(p => p.ProductType == ProductKinds.Component).AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(p => p.Name.Contains(keyword.Trim()));
        if (!string.IsNullOrWhiteSpace(componentType)) query = query.Where(p => p.ComponentType == componentType);
        if (!string.IsNullOrWhiteSpace(brand)) query = query.Where(p => p.Brand == brand);
        if (minPrice.HasValue) query = query.Where(p => ((p.DiscountPrice ?? p.SalePrice) ?? p.Price) >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(p => ((p.DiscountPrice ?? p.SalePrice) ?? p.Price) <= maxPrice.Value);
        if (isActive.HasValue) query = query.Where(p => p.IsActive == isActive.Value);
        if (inStock.HasValue) query = query.Where(p => (p.StockQuantity > 0) == inStock.Value);

        var vm = new AdminComponentIndexVm
        {
            Components = await query.OrderByDescending(p => p.CreatedAt).ToListAsync(),
            BrandOptions = await _db.Products.Where(p => p.ProductType == ProductKinds.Component && p.Brand != null && p.Brand != "")
                .Select(p => p.Brand!).Distinct().OrderBy(x => x).ToListAsync(),
            Keyword = keyword, ComponentType = componentType, Brand = brand, MinPrice = minPrice, MaxPrice = maxPrice, IsActive = isActive, InStock = inStock
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create() => View(await BuildVmAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminComponentUpsertVm vm)
    {
        await PopulateCategoriesAsync(vm);
        TryValidateProductImageUrls(vm, true);
        NormalizeComponentInput(vm);
        if (!ModelState.IsValid) return InvalidForm(vm, "tạo");

        var product = new Product
        {
            Name = vm.Name,
            ProductCode = string.IsNullOrWhiteSpace(vm.ProductCode) ? $"LK-{Guid.NewGuid():N}"[..16] : vm.ProductCode.Trim(),
            Brand = string.IsNullOrWhiteSpace(vm.Brand) ? null : vm.Brand.Trim(),
            ProductType = ProductKinds.Component,
            ComponentType = vm.ComponentType,
            Price = vm.Price!.Value,
            DiscountPrice = vm.DiscountPrice,
            SalePrice = vm.DiscountPrice,
            StockQuantity = vm.StockQuantity.GetValueOrDefault(),
            WarrantyMonths = vm.WarrantyMonths!.Value,
            WarrantyDuration = $"{vm.WarrantyMonths.Value} tháng",
            CategoryId = vm.CategoryId!.Value,
            ShortDescription = BuildShortDescription(vm.Description),
            Description = vm.Description ?? string.Empty,
            DetailDescription = vm.Description ?? string.Empty,
            Specifications = ProductSpecificationKeyValueHelper.Serialize(vm.SpecificationItems),
            IsActive = vm.IsActive,
            IsInStock = vm.StockQuantity.GetValueOrDefault() > 0,
            Slug = BuildSlug(vm.Name),
            ThumbnailImage = vm.ThumbnailImageUrl!.Trim()
        };
        AddProductImagesFromUrls(product, vm.ProductImageUrlsText);
        EnsurePrimaryImage(product);
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Đã thêm linh kiện thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vm = await BuildVmAsync(id);
        return vm == null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminComponentUpsertVm vm)
    {
        await PopulateCategoriesAsync(vm);
        TryValidateProductImageUrls(vm, false);
        NormalizeComponentInput(vm);
        if (!ModelState.IsValid) { await PopulateExistingImagesAsync(vm); return InvalidForm(vm, "cập nhật"); }
        var product = await _db.Products.Include(p => p.ProductImages).FirstOrDefaultAsync(p => p.Id == vm.Id && p.ProductType == ProductKinds.Component);
        if (product == null) return NotFound();
        product.Name = vm.Name; product.ProductCode = string.IsNullOrWhiteSpace(vm.ProductCode) ? product.ProductCode : vm.ProductCode.Trim(); product.Brand = string.IsNullOrWhiteSpace(vm.Brand) ? null : vm.Brand.Trim(); product.ProductType = ProductKinds.Component; product.ComponentType = vm.ComponentType;
        product.Price = vm.Price!.Value; product.DiscountPrice = vm.DiscountPrice; product.SalePrice = vm.DiscountPrice; product.StockQuantity = vm.StockQuantity.GetValueOrDefault();
        product.WarrantyMonths = vm.WarrantyMonths!.Value; product.WarrantyDuration = $"{vm.WarrantyMonths.Value} tháng"; product.CategoryId = vm.CategoryId!.Value;
        product.ShortDescription = BuildShortDescription(vm.Description); product.Description = vm.Description ?? string.Empty; product.DetailDescription = vm.Description ?? string.Empty;
        product.Specifications = ProductSpecificationKeyValueHelper.Serialize(vm.SpecificationItems); product.IsActive = vm.IsActive; product.IsInStock = vm.StockQuantity.GetValueOrDefault() > 0; product.Slug = BuildSlug(vm.Name);
        if (vm.RemoveImageIds.Any()) foreach (var image in product.ProductImages.Where(x => vm.RemoveImageIds.Contains(x.Id)).ToList()) _db.ProductImages.Remove(image);
        ApplyExistingImageOrder(product, vm.ExistingImageOrder);
        if (!string.IsNullOrWhiteSpace(vm.ThumbnailImageUrl)) product.ThumbnailImage = vm.ThumbnailImageUrl.Trim();
        AddProductImagesFromUrls(product, vm.ProductImageUrlsText); EnsurePrimaryImage(product);
        await _db.SaveChangesAsync(); TempData["Ok"] = "Đã cập nhật linh kiện."; return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.Include(p => p.ProductImages).FirstOrDefaultAsync(p => p.Id == id && p.ProductType == ProductKinds.Component);
        if (product != null) { _db.Products.Remove(product); await _db.SaveChangesAsync(); TempData["Ok"] = "Đã xóa linh kiện."; }
        return RedirectToAction(nameof(Index));
    }

    private async Task<AdminComponentUpsertVm?> BuildVmAsync(int? id = null)
    {
        var vm = new AdminComponentUpsertVm(); await PopulateCategoriesAsync(vm); if (!id.HasValue) return vm;
        var p = await _db.Products.Include(x => x.ProductImages).FirstOrDefaultAsync(x => x.Id == id.Value && x.ProductType == ProductKinds.Component); if (p == null) return null;
        vm.Id = p.Id; vm.Name = p.Name; vm.ProductCode = p.ProductCode; vm.Brand = p.Brand; vm.ComponentType = string.IsNullOrWhiteSpace(p.ComponentType) ? ComponentTypes.Other : p.ComponentType; vm.Price = p.Price; vm.DiscountPrice = p.DiscountPrice ?? p.SalePrice; vm.StockQuantity = p.StockQuantity; vm.WarrantyMonths = p.WarrantyMonths; vm.CategoryId = p.CategoryId; vm.Description = p.Description; vm.Specifications = p.Specifications; vm.SpecificationItems = ProductSpecificationKeyValueHelper.ParseStored(p.Specifications); vm.IsActive = p.IsActive; vm.ThumbnailImageUrl = p.ThumbnailImage;
        var imgs = p.ProductImages.OrderBy(x => x.SortOrder).ToList(); vm.ExistingImageOrder = imgs.Select(x => x.Id).ToList(); vm.ExistingImages = imgs.Select(x => new ProductImageItemVm{Id=x.Id, ImageUrl=x.ImageUrl, IsPrimary=x.IsPrimary, SortOrder=x.SortOrder}).ToList(); return vm;
    }
    private async Task PopulateCategoriesAsync(AdminProductUpsertVm vm) => vm.Categories = await _db.Categories.OrderBy(x => x.Name).ToListAsync();
    private async Task PopulateExistingImagesAsync(AdminProductUpsertVm vm) { vm.ExistingImages = await _db.ProductImages.Where(x => x.ProductId == vm.Id).OrderBy(x => x.SortOrder).Select(x => new ProductImageItemVm{Id=x.Id,ImageUrl=x.ImageUrl,IsPrimary=x.IsPrimary,SortOrder=x.SortOrder}).ToListAsync(); if (!vm.ExistingImageOrder.Any()) vm.ExistingImageOrder = vm.ExistingImages.Select(x => x.Id).ToList(); }
    private IActionResult InvalidForm(AdminComponentUpsertVm vm, string op) { _logger.LogWarning("Không thể {Operation} linh kiện: ModelState invalid", op); TempData["ErrorMessage"] = $"Không thể {op} linh kiện. Vui lòng kiểm tra lỗi trong biểu mẫu."; return View(vm); }
    private bool TryValidateProductImageUrls(AdminProductUpsertVm vm, bool requireThumbnail) { if (requireThumbnail && string.IsNullOrWhiteSpace(vm.ThumbnailImageUrl)) ModelState.AddModelError(nameof(vm.ThumbnailImageUrl), "Vui lòng nhập URL ảnh đại diện."); ValidateUrl(vm.ThumbnailImageUrl, nameof(vm.ThumbnailImageUrl)); foreach (var url in SplitImageUrls(vm.ProductImageUrlsText)) ValidateUrl(url, nameof(vm.ProductImageUrlsText)); return ModelState.IsValid; }
    private void ValidateUrl(string? value, string key) { if (string.IsNullOrWhiteSpace(value)) return; if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) ModelState.AddModelError(key, "Chỉ chấp nhận URL http/https hợp lệ."); }
    private void NormalizeComponentInput(AdminComponentUpsertVm vm)
    {
        vm.ProductType = ProductKinds.Component;
        vm.ComponentType = string.IsNullOrWhiteSpace(vm.ComponentType) ? ComponentTypes.Other : vm.ComponentType.Trim();
        vm.Name = vm.Name?.Trim() ?? string.Empty;
        vm.Brand = string.IsNullOrWhiteSpace(vm.Brand) ? null : vm.Brand.Trim();
        vm.Description = vm.Description?.Trim();
        if (!vm.StockQuantity.HasValue)
        {
            vm.StockQuantity = 0;
            ModelState.Remove(nameof(vm.StockQuantity));
        }
        if (!vm.WarrantyMonths.HasValue)
        {
            vm.WarrantyMonths = 0;
            ModelState.Remove(nameof(vm.WarrantyMonths));
        }
        vm.DiscountPrice = vm.DiscountPrice.GetValueOrDefault() > 0 ? vm.DiscountPrice : null;
        vm.SpecificationItems = ProductSpecificationKeyValueHelper.ParseStored(ProductSpecificationKeyValueHelper.Serialize(vm.SpecificationItems));
        vm.Specifications = ProductSpecificationKeyValueHelper.Serialize(vm.SpecificationItems);
    }
    private static List<string> SplitImageUrls(string? urlsText) => (urlsText ?? string.Empty).Split(new[] {'\r','\n',',',';'}, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    private void AddProductImagesFromUrls(Product product, string? text) { var sort = product.ProductImages.Count == 0 ? 1 : product.ProductImages.Max(x => x.SortOrder) + 1; foreach (var url in SplitImageUrls(text)) product.ProductImages.Add(new ProductImage{ImageUrl=url, SortOrder=sort++}); }
    private static void ApplyExistingImageOrder(Product product, List<int> ids) { var sort=1; foreach(var id in ids){var img=product.ProductImages.FirstOrDefault(x=>x.Id==id); if(img!=null) img.SortOrder=sort++;} }
    private static void EnsurePrimaryImage(Product product) { var ordered=product.ProductImages.OrderBy(x=>x.SortOrder).ToList(); if(!ordered.Any()) return; foreach(var img in ordered) img.IsPrimary=false; ordered[0].IsPrimary=true; if(string.IsNullOrWhiteSpace(product.ThumbnailImage)) product.ThumbnailImage=ordered[0].ImageUrl; }
    private static string BuildSlug(string input) => string.IsNullOrWhiteSpace(input) ? Guid.NewGuid().ToString("N") : string.Join('-', input.ToLowerInvariant().Trim().Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries));
    private static string BuildShortDescription(string? description) => string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim().Split(new[] {'\r','\n'}, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? string.Empty;
}
