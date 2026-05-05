using Datn.PcStore.Data;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

public class ProductsController : Controller
{
    private readonly ApplicationDbContext _db;
    public ProductsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(ProductFilterVm vm)
    {
        var query = _db.Products.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(vm.Keyword))
            query = query.Where(p => p.Name.Contains(vm.Keyword));
        if (vm.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == vm.CategoryId.Value);
        if (!string.IsNullOrWhiteSpace(vm.Brand))
            query = query.Where(p => p.Brand == vm.Brand);
        if (vm.MinPrice.HasValue)
            query = query.Where(p => ((p.DiscountPrice ?? p.SalePrice) ?? p.Price) >= vm.MinPrice.Value);
        if (vm.MaxPrice.HasValue)
            query = query.Where(p => ((p.DiscountPrice ?? p.SalePrice) ?? p.Price) <= vm.MaxPrice.Value);

        vm.Categories = await _db.Categories.ToListAsync();
        vm.Products = await query
            .Include(p => p.ProductImages)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return View(vm);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.ProductImages.OrderBy(x => x.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        ViewBag.UpgradeSuggestions = await _db.Products
            .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.Price > product.Price)
            .OrderBy(p => p.Price)
            .Take(3)
            .ToListAsync();

        return View(product);
    }
}
