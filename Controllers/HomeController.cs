using Datn.PcStore.Data;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    public HomeController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var utcNow = DateTime.UtcNow;
        var categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        var vm = new HomeIndexVm
        {
            Categories = categories,
            SiteSettings = await _db.SiteSettings.OrderBy(x => x.Id).FirstOrDefaultAsync(),
            Banners = await _db.Banners.Where(b => b.IsActive).OrderBy(b => b.SortOrder).ToListAsync(),
            HotSaleProducts = await _db.Products.Include(p => p.ProductImages).Where(p => p.IsActive && p.IsHotSale && (!p.PromotionStartDate.HasValue || p.PromotionStartDate <= utcNow) && (!p.PromotionEndDate.HasValue || p.PromotionEndDate >= utcNow)).OrderByDescending(p => p.CreatedAt).Take(20).ToListAsync(),
            DailyDealProducts = await _db.Products.Include(p => p.ProductImages).Where(p => p.IsActive && p.IsDailyDeal && (!p.PromotionStartDate.HasValue || p.PromotionStartDate <= utcNow) && (!p.PromotionEndDate.HasValue || p.PromotionEndDate >= utcNow)).OrderByDescending(p => p.CreatedAt).Take(20).ToListAsync(),
            PromotionProducts = await _db.Products.Include(p => p.ProductImages).Where(p => p.IsActive && p.IsPromotion && (!p.PromotionStartDate.HasValue || p.PromotionStartDate <= utcNow) && (!p.PromotionEndDate.HasValue || p.PromotionEndDate >= utcNow)).OrderByDescending(p => p.CreatedAt).Take(20).ToListAsync(),
            PcGamingProducts = await GetByCategoryNameAsync("PC Gaming"),
            LaptopProducts = await GetByCategoryNameAsync("Laptop"),
            MonitorProducts = await GetByCategoryNameAsync("Màn hình"),
            ComponentProducts = await GetByCategoryNameAsync("Linh kiện"),
            WorkstationProducts = await GetByCategoryNameAsync("Workstation"),
            AmdGamingProducts = await GetByCategoryNameAsync("AMD Gaming"),
            PcMiniProducts = await GetByCategoryNameAsync("PC Mini"),
            OfficePcProducts = await GetByCategoryNameAsync("PC Văn Phòng")
        };

        ViewBag.Categories = categories;
        return View(vm);
    }

    private async Task<List<Models.Product>> GetByCategoryNameAsync(string categoryName)
    {
        var categoryId = await _db.Categories.Where(c => c.Name == categoryName).Select(c => c.Id).FirstOrDefaultAsync();
        if (categoryId == 0)
        {
            return new List<Models.Product>();
        }

        return await _db.Products.Include(p => p.ProductImages)
            .Where(p => p.IsActive && p.CategoryId == categoryId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(20)
            .ToListAsync();
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}
