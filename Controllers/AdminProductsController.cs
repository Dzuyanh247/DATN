using Datn.PcStore.Data;
using Datn.PcStore.Helpers;
using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
public class AdminProductsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AdminProductsController> _logger;

    public AdminProductsController(ApplicationDbContext db, ILogger<AdminProductsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, int? categoryId)
    {
        var query = _db.Products.Include(p => p.Category).Include(p => p.ProductImages).Where(p => p.ProductType == ProductKinds.PC).AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => (x.Name ?? string.Empty).Contains(keyword));
        if (categoryId.HasValue) query = query.Where(x => x.CategoryId == categoryId);
        ViewBag.Keyword = keyword;
        ViewBag.CategoryId = categoryId;
        ViewBag.Categories = await _db.Categories.OrderBy(x => x.Name ?? string.Empty).ToListAsync();
        return View(await query.OrderByDescending(p => p.CreatedAt).ToListAsync());
    }

    [HttpGet] public async Task<IActionResult> Create() => View(await BuildUpsertVmAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminProductUpsertVm vm)
    {
        await PopulateCategoriesAsync(vm);
        NormalizePcOnlyFields(vm);
        ParseAndValidatePrices(vm);
        TryValidateProductImageUrls(vm, true);
        var promotionText = BuildAndValidatePromotionText(vm);
        if (!ModelState.IsValid) return InvalidProductForm(vm, "tạo");

        var price = vm.Price!.Value;
        var stockQuantity = vm.StockQuantity!.Value;
        var warrantyMonths = vm.WarrantyMonths!.Value;
        var categoryId = vm.CategoryId!.Value;
        var slug = BuildSlug(vm.Name);

        var product = new Product
        {
            Name = vm.Name.Trim(),
            ProductCode = $"SP-{Guid.NewGuid():N}"[..16],
            Brand = null,
            ProductType = ProductKinds.PC,
            Price = price,
            DiscountPrice = vm.DiscountPrice,
            SalePrice = vm.DiscountPrice,
            IsHotSale = vm.IsHotSale,
            IsDailyDeal = vm.IsDailyDeal,
            IsPromotion = vm.IsPromotion,
            PromotionStartDate = vm.PromotionStartDate,
            PromotionEndDate = vm.PromotionEndDate,
            PromotionText = promotionText,
            StockQuantity = stockQuantity,
            WarrantyMonths = warrantyMonths,
            WarrantyDuration = $"{warrantyMonths} tháng",
            CategoryId = categoryId,
            ShortDescription = BuildShortDescription(vm.Description),
            Description = vm.Description ?? string.Empty,
            DetailDescription = vm.Description ?? string.Empty,
            TechnicalSpecifications = ProductComponentSpecHelper.Serialize(vm.ComponentSpecs),
            ComponentType = ProductKinds.PC,
            IsActive = vm.IsActive,
            IsInStock = stockQuantity > 0,
            Slug = slug,
            ThumbnailImage = ResolveThumbnailUrl(vm)
        };

        if (!await ValidateUniqueProductFieldsAsync(vm, product.Slug, product.ProductCode, "tạo")) return View(vm);

        AddProductImagesFromUrls(product, vm.ProductImageUrlsText);
        EnsurePrimaryImage(product);

        _db.Products.Add(product);
        if (!await TrySaveProductChangesAsync(vm, "tạo")) return View(vm);
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
        NormalizePcOnlyFields(vm);
        ParseAndValidatePrices(vm);
        TryValidateProductImageUrls(vm, false);
        var promotionText = BuildAndValidatePromotionText(vm);
        if (!ModelState.IsValid)
        {
            await PopulateExistingImagesAsync(vm);
            return InvalidProductForm(vm, "cập nhật");
        }

        var product = await _db.Products.Include(p => p.ProductImages).FirstOrDefaultAsync(x => x.Id == vm.Id && x.ProductType == ProductKinds.PC);
        if (product == null) return NotFound();

        var slug = BuildSlug(vm.Name);
        var productCode = string.IsNullOrWhiteSpace(product.ProductCode) ? $"SP-{Guid.NewGuid():N}"[..16] : product.ProductCode.Trim();
        if (!await ValidateUniqueProductFieldsAsync(vm, slug, productCode, "cập nhật"))
        {
            await PopulateExistingImagesAsync(vm);
            return View(vm);
        }

        product.Name = vm.Name.Trim();
        product.ProductCode = productCode;
        product.Brand = null;
        product.ProductType = ProductKinds.PC;
        product.ComponentType = ProductKinds.PC;
        product.Price = vm.Price!.Value;
        product.DiscountPrice = vm.DiscountPrice;
        product.SalePrice = vm.DiscountPrice;
        product.IsHotSale = vm.IsHotSale;
        product.IsDailyDeal = vm.IsDailyDeal;
        product.IsPromotion = vm.IsPromotion;
        product.PromotionStartDate = vm.PromotionStartDate;
        product.PromotionEndDate = vm.PromotionEndDate;
        product.PromotionText = promotionText;
        product.StockQuantity = vm.StockQuantity!.Value;
        product.WarrantyMonths = vm.WarrantyMonths!.Value;
        product.WarrantyDuration = $"{vm.WarrantyMonths.Value} tháng";
        product.CategoryId = vm.CategoryId!.Value;
        product.ShortDescription = BuildShortDescription(vm.Description);
        product.Description = vm.Description ?? string.Empty;
        product.DetailDescription = vm.Description ?? string.Empty;
        product.TechnicalSpecifications = ProductComponentSpecHelper.Serialize(vm.ComponentSpecs);
        product.IsActive = vm.IsActive;
        product.IsInStock = vm.StockQuantity.Value > 0;
        product.Slug = slug;
        product.UpdatedAt = DateTime.UtcNow;

        if (vm.RemoveImageIds.Any())
        {
            var imagesToDelete = product.ProductImages.Where(x => vm.RemoveImageIds.Contains(x.Id)).ToList();
            foreach (var image in imagesToDelete) _db.ProductImages.Remove(image);
        }

        ApplyExistingImageOrder(product, vm.ExistingImageOrder);
        SyncProductImagesFromTextarea(product, vm.ProductImageUrlsText);
        if (!string.IsNullOrWhiteSpace(vm.ThumbnailImageUrl)) product.ThumbnailImage = vm.ThumbnailImageUrl.Trim();
        EnsurePrimaryImage(product);

        if (!await TrySaveProductChangesAsync(vm, "cập nhật"))
        {
            await PopulateExistingImagesAsync(vm);
            return View(vm);
        }
        TempData["Ok"] = "Đã cập nhật sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.Include(p => p.ProductImages).FirstOrDefaultAsync(x => x.Id == id && x.ProductType == ProductKinds.PC);
        if (product == null) return RedirectToAction(nameof(Index));
        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Đã xóa sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<AdminProductUpsertVm?> BuildUpsertVmAsync(int? productId = null) { /* omitted for brevity */
        var vm = new AdminProductUpsertVm(); await PopulateCategoriesAsync(vm); if (!productId.HasValue) return vm;
        var product = await _db.Products.Include(p => p.ProductImages).FirstOrDefaultAsync(x => x.Id == productId.Value && x.ProductType == ProductKinds.PC); if (product == null) return null;
        vm.Id = product.Id; vm.Name = product.Name ?? string.Empty; vm.Brand = product.Brand; vm.ProductType = product.ProductType ?? ProductKinds.PC; vm.ComponentType = ProductKinds.PC; vm.Price = product.Price; vm.PriceInput = FormatMoneyForInput(product.Price); vm.DiscountPrice = product.DiscountPrice ?? product.SalePrice; vm.DiscountPriceInput = FormatMoneyForInput(vm.DiscountPrice);
        vm.IsHotSale = product.IsHotSale; vm.IsDailyDeal = product.IsDailyDeal; vm.IsPromotion = product.IsPromotion;
        vm.PromotionStartDate = product.PromotionStartDate; vm.PromotionEndDate = product.PromotionEndDate;
        vm.SelectedPromotionTexts = new List<string>(); vm.CustomPromotionText = product.PromotionText;
        vm.StockQuantity = product.StockQuantity; vm.WarrantyMonths = product.WarrantyMonths > 0 ? product.WarrantyMonths : 12; vm.CategoryId = product.CategoryId;
        vm.Description = ResolveDescriptionForEditing(product); vm.Specifications = product.TechnicalSpecifications; vm.ComponentSpecs = ProductComponentSpecHelper.ParseStored(product.TechnicalSpecifications); vm.IsActive = product.IsActive;
        var orderedImages = product.ProductImages.OrderBy(x => x.SortOrder).ToList();
        vm.ThumbnailImageUrl = orderedImages.FirstOrDefault(x => x.IsPrimary)?.ImageUrl ?? orderedImages.FirstOrDefault()?.ImageUrl ?? product.ThumbnailImage ?? string.Empty;
        vm.ProductImageUrlsText = string.Join(Environment.NewLine, orderedImages.Select(x => x.ImageUrl));
        vm.ExistingImageOrder = orderedImages.Select(x => x.Id).ToList();
        vm.ExistingImages = orderedImages.Select(x => new ProductImageItemVm { Id = x.Id, ImageUrl = x.ImageUrl, IsPrimary = x.IsPrimary, SortOrder = x.SortOrder }).ToList(); return vm; }

    private IActionResult InvalidProductForm(AdminProductUpsertVm vm, string operation)
    {
        var errors = ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors.Select(error => new
            {
                Field = string.IsNullOrWhiteSpace(entry.Key) ? "Model" : entry.Key,
                Message = string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? error.Exception?.Message ?? "Giá trị không hợp lệ."
                    : error.ErrorMessage
            }))
            .ToList();

        _logger.LogWarning(
            "Không thể {Operation} sản phẩm vì ModelState không hợp lệ: {@ValidationErrors}",
            operation,
            errors);
        TempData["ErrorMessage"] = $"Không thể {operation} sản phẩm. Vui lòng kiểm tra {errors.Count} lỗi được hiển thị trong biểu mẫu.";
        return View(vm);
    }

    private static string? NormalizeNullableText(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private string? BuildAndValidatePromotionText(AdminProductUpsertVm vm)
    {
        var promotionText = ProductPromotionHelper.BuildStoredText(vm.SelectedPromotionTexts, vm.CustomPromotionText);
        if (promotionText.Length > ProductPromotionHelper.MaxStoredLength)
        {
            ModelState.AddModelError(nameof(vm.CustomPromotionText), $"Tổng nội dung khuyến mại không được vượt quá {ProductPromotionHelper.MaxStoredLength:N0} ký tự.");
        }

        return NormalizeNullableText(promotionText);
    }

    private async Task PopulateCategoriesAsync(AdminProductUpsertVm vm) => vm.Categories = await _db.Categories.OrderBy(x => x.Name ?? string.Empty).ToListAsync();

    private async Task<bool> ValidateUniqueProductFieldsAsync(AdminProductUpsertVm vm, string slug, string productCode, string operation)
    {
        var name = vm.Name.Trim();
        var normalizedName = name.ToLower();
        var normalizedSlug = slug.ToLower();
        var normalizedProductCode = productCode.Trim().ToLower();

        var duplicate = await _db.Products.AsNoTracking()
            .Where(p => p.Id != vm.Id)
            .Where(p =>
                (p.Name ?? string.Empty).ToLower() == normalizedName ||
                (p.Slug ?? string.Empty).ToLower() == normalizedSlug ||
                (p.ProductCode ?? string.Empty).ToLower() == normalizedProductCode)
            .Select(p => new { p.Name, p.Slug, p.ProductCode })
            .FirstOrDefaultAsync();

        if (duplicate == null) return true;

        if (string.Equals(duplicate.Name, name, StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(vm.Name), "Tên sản phẩm đã tồn tại.");
        if (string.Equals(duplicate.Slug, slug, StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(vm.Name), "Slug sản phẩm đã tồn tại. Vui lòng đổi tên sản phẩm.");
        if (string.Equals(duplicate.ProductCode, productCode, StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(vm.ProductCode), "Mã sản phẩm đã tồn tại.");

        InvalidProductForm(vm, operation);
        return false;
    }

    private async Task<bool> TrySaveProductChangesAsync(AdminProductUpsertVm vm, string operation)
    {
        try
        {
            await _db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (IsProductUniqueConstraintViolation(ex))
        {
            _logger.LogWarning(ex, "Không thể {Operation} sản phẩm vì trùng dữ liệu unique trong database.", operation);
            AddProductUniqueConstraintModelError(vm, ex);
            InvalidProductForm(vm, operation);
            return false;
        }
    }

    private static bool IsProductUniqueConstraintViolation(DbUpdateException ex)
    {
        var message = ex.GetBaseException().Message;
        return message.Contains("IX_Products_ProductCode", StringComparison.OrdinalIgnoreCase)
            || message.Contains("IX_Products_Slug", StringComparison.OrdinalIgnoreCase)
            || message.Contains("ProductCode", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Slug", StringComparison.OrdinalIgnoreCase)
            || message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
    }

    private void AddProductUniqueConstraintModelError(AdminProductUpsertVm vm, DbUpdateException ex)
    {
        var message = ex.GetBaseException().Message;
        if (message.Contains("ProductCode", StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(vm.ProductCode), "Mã sản phẩm đã tồn tại.");
        else if (message.Contains("Slug", StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(vm.Name), "Slug sản phẩm đã tồn tại. Vui lòng đổi tên sản phẩm.");
        else
            ModelState.AddModelError(string.Empty, "Dữ liệu sản phẩm bị trùng. Vui lòng kiểm tra tên sản phẩm, slug hoặc mã sản phẩm.");
    }

    private void NormalizePcOnlyFields(AdminProductUpsertVm vm)
    {
        vm.ProductType = ProductKinds.PC;
        vm.Brand = null;
        vm.ComponentType = ProductKinds.PC;
        vm.Name = vm.Name?.Trim() ?? string.Empty;
        ModelState.Remove(nameof(vm.Brand));
        ModelState.Remove(nameof(vm.ComponentType));
        ModelState.Remove(nameof(vm.Price));
        ModelState.Remove(nameof(vm.DiscountPrice));
    }

    private void ParseAndValidatePrices(AdminProductUpsertVm vm)
    {
        vm.PriceInput ??= FormatMoneyForInput(vm.Price);
        vm.DiscountPriceInput ??= FormatMoneyForInput(vm.DiscountPrice);
        if (!TryParseMoney(vm.PriceInput, out var price) || price < 1000 || price > 999999999)
            ModelState.AddModelError(nameof(vm.PriceInput), "Giá gốc phải từ 1.000 đến 999.999.999 và chỉ nhập số/dấu phân tách hợp lệ.");
        else
            vm.Price = price;

        if (string.IsNullOrWhiteSpace(vm.DiscountPriceInput))
        {
            vm.DiscountPrice = null;
        }
        else if (!TryParseMoney(vm.DiscountPriceInput, out var discountPrice) || discountPrice < 0 || discountPrice > 999999999)
        {
            ModelState.AddModelError(nameof(vm.DiscountPriceInput), "Giá khuyến mãi không hợp lệ hoặc vượt quá 999.999.999.");
        }
        else
        {
            vm.DiscountPrice = discountPrice;
        }
    }

    private static string FormatMoneyForInput(decimal? value) => value.HasValue ? decimal.Truncate(value.Value).ToString("0", CultureInfo.InvariantCulture) : string.Empty;

    private static bool TryParseMoney(string? value, out decimal result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var text = value.Trim().Replace(" ", string.Empty);
        var lastComma = text.LastIndexOf(',');
        var lastDot = text.LastIndexOf('.');
        if (lastComma >= 0 && lastDot >= 0)
        {
            var decimalSeparator = lastComma > lastDot ? ',' : '.';
            var thousandsSeparator = decimalSeparator == ',' ? '.' : ',';
            text = text.Replace(thousandsSeparator.ToString(), string.Empty).Replace(decimalSeparator, '.');
        }
        else if (lastComma >= 0 || lastDot >= 0)
        {
            var separator = lastComma >= 0 ? ',' : '.';
            var index = Math.Max(lastComma, lastDot);
            var decimals = text.Length - index - 1;
            if (decimals == 2) text = text.Replace(separator, '.');
            else text = text.Replace(separator.ToString(), string.Empty);
        }
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }
    private async Task PopulateExistingImagesAsync(AdminProductUpsertVm vm) { vm.ExistingImages = await _db.ProductImages.Where(x => x.ProductId == vm.Id).OrderBy(x => x.SortOrder).Select(x => new ProductImageItemVm { Id = x.Id, ImageUrl = x.ImageUrl, IsPrimary = x.IsPrimary, SortOrder = x.SortOrder }).ToListAsync(); if (!vm.ExistingImageOrder.Any()) vm.ExistingImageOrder = vm.ExistingImages.Select(x => x.Id).ToList(); }

    private bool TryValidateProductImageUrls(AdminProductUpsertVm vm, bool requireThumbnail)
    {
        if (requireThumbnail && string.IsNullOrWhiteSpace(vm.ThumbnailImageUrl) && !SplitImageUrls(vm.ProductImageUrlsText).Any()) ModelState.AddModelError(nameof(vm.ThumbnailImageUrl), "Vui lòng nhập URL thumbnail hoặc ít nhất một URL trong thư viện ảnh.");
        ValidateUrl(vm.ThumbnailImageUrl, nameof(vm.ThumbnailImageUrl));
        foreach (var url in SplitImageUrls(vm.ProductImageUrlsText)) ValidateUrl(url, nameof(vm.ProductImageUrlsText));
        return ModelState.IsValid;
    }

    private static string ResolveThumbnailUrl(AdminProductUpsertVm vm) => !string.IsNullOrWhiteSpace(vm.ThumbnailImageUrl)
        ? vm.ThumbnailImageUrl.Trim()
        : SplitImageUrls(vm.ProductImageUrlsText).FirstOrDefault() ?? string.Empty;

    private void AddProductImagesFromUrls(Product product, string? urlsText)
    {
        var sort = product.ProductImages.Count == 0 ? 1 : product.ProductImages.Max(x => x.SortOrder) + 1;
        foreach (var url in SplitImageUrls(urlsText)) product.ProductImages.Add(new ProductImage { ImageUrl = url, SortOrder = sort++, IsPrimary = false });
    }

    private void SyncProductImagesFromTextarea(Product product, string? urlsText)
    {
        var submittedUrls = SplitImageUrls(urlsText);
        if (!submittedUrls.Any()) return;

        var existingUrls = product.ProductImages
            .OrderBy(x => x.SortOrder)
            .Select(x => x.ImageUrl)
            .ToList();
        if (existingUrls.SequenceEqual(submittedUrls, StringComparer.OrdinalIgnoreCase)) return;

        foreach (var image in product.ProductImages.ToList())
            _db.ProductImages.Remove(image);
        product.ProductImages.Clear();

        var sort = 1;
        foreach (var url in submittedUrls)
            product.ProductImages.Add(new ProductImage { ProductId = product.Id, ImageUrl = url, SortOrder = sort++, IsPrimary = false });
    }

    private static List<string> SplitImageUrls(string? urlsText) => (urlsText ?? string.Empty).Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    private void ValidateUrl(string? value, string key) { if (string.IsNullOrWhiteSpace(value)) return; if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) ModelState.AddModelError(key, "Chỉ chấp nhận URL http/https hợp lệ."); }
    private static void ApplyExistingImageOrder(Product product, List<int> orderedImageIds) { if (!orderedImageIds.Any()) return; var current = product.ProductImages.ToDictionary(x => x.Id); var sort = 1; foreach (var imageId in orderedImageIds) if (current.TryGetValue(imageId, out var image)) image.SortOrder = sort++; foreach (var image in product.ProductImages.Where(x => x.SortOrder <= 0).OrderBy(x => x.Id)) image.SortOrder = sort++; }
    private static void EnsurePrimaryImage(Product product) { var orderedImages = product.ProductImages.OrderBy(x => x.SortOrder).ToList(); if (!orderedImages.Any()) return; var primaryImage = orderedImages.First(); foreach (var image in orderedImages) image.IsPrimary = image.Id == primaryImage.Id; if (string.IsNullOrWhiteSpace(product.ThumbnailImage)) product.ThumbnailImage = primaryImage.ImageUrl; }
    private static string BuildSlug(string input) => string.IsNullOrWhiteSpace(input) ? Guid.NewGuid().ToString("N") : string.Join('-', input.ToLowerInvariant().Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
    private static string ResolveDescriptionForEditing(Product product)
    {
        if (!string.IsNullOrWhiteSpace(product.Description)) return product.Description;
        if (!string.IsNullOrWhiteSpace(product.DetailDescription)) return product.DetailDescription;
        return product.ShortDescription ?? string.Empty;
    }

    private static string BuildShortDescription(string? description) => string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? string.Empty;
}
