using System.Security.Claims;
using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Datn.PcStore.Services;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize]
public class BuildPcController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICartService _cartService;

    private readonly string[] _componentTypes = ["CPU", "Mainboard", "RAM", "SSD", "GPU", "PSU", "Case"];

    public BuildPcController(ApplicationDbContext db, ICartService cartService)
    {
        _db = db;
        _cartService = cartService;
    }

    [HttpGet]
    public async Task<IActionResult> Index() => View(await BuildVmAsync(new Dictionary<string, int?>()));

    [HttpPost]
    public async Task<IActionResult> Index(Dictionary<string, int?> selected)
    {
        return View(await BuildVmAsync(selected));
    }

    [HttpPost]
    public async Task<IActionResult> AddConfigToCart(Dictionary<string, int?> selected)
    {
        var vm = await BuildVmAsync(selected);
        if (!vm.IsCompatible)
        {
            TempData["Err"] = vm.CompatibilityMessage;
            return RedirectToAction(nameof(Index));
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        foreach (var p in vm.SelectedProducts.Values.Where(p => p != null).Cast<Product>())
        {
            await _cartService.AddToCartAsync(userId, p.Id, 1);
        }

        TempData["Ok"] = "Đã thêm toàn bộ cấu hình vào giỏ hàng.";
        return RedirectToAction("Index", "Cart");
    }


    [HttpPost]
    public async Task<IActionResult> SaveConfig(Dictionary<string, int?> selected, string? configName)
    {
        var vm = await BuildVmAsync(selected);
        if (!vm.SelectedProducts.Values.Any(p => p != null))
        {
            TempData["Err"] = "Bạn chưa chọn linh kiện để lưu cấu hình.";
            return RedirectToAction(nameof(Index));
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var config = new BuildPcConfig
        {
            UserId = userId,
            Name = string.IsNullOrWhiteSpace(configName) ? $"Cấu hình {DateTime.Now:dd/MM HH:mm}" : configName.Trim(),
            TotalPrice = vm.TotalPrice
        };
        _db.BuildPcConfigs.Add(config);
        await _db.SaveChangesAsync();

        foreach (var component in vm.SelectedProducts)
        {
            if (component.Value == null) continue;
            _db.BuildPcItems.Add(new BuildPcItem
            {
                BuildPcConfigId = config.Id,
                ComponentType = component.Key,
                ProductId = component.Value.Id
            });
        }
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Đã lưu cấu hình Build PC.";
        return RedirectToAction(nameof(Index));
    }
    private async Task<BuildPcVm> BuildVmAsync(Dictionary<string, int?> selected)
    {
        var vm = new BuildPcVm { Selected = selected };

        foreach (var type in _componentTypes)
        {
            vm.Components[type] = await _db.Products.Where(p => p.ComponentType == type && p.IsInStock).OrderBy(p => p.Name).ToListAsync();
            selected.TryGetValue(type, out var productId);
            vm.SelectedProducts[type] = productId.HasValue ? await _db.Products.FindAsync(productId.Value) : null;
            if (vm.SelectedProducts[type] != null)
            {
                vm.TotalPrice += vm.SelectedProducts[type]!.DiscountPrice ??  vm.SelectedProducts[type]!.Price;
            }
        }

        var cpu = vm.SelectedProducts["CPU"];
        var mainboard = vm.SelectedProducts["Mainboard"];
        var ram = vm.SelectedProducts["RAM"];

        if (cpu != null && mainboard != null && !string.Equals(cpu.CpuSocket, mainboard.CpuSocket, StringComparison.OrdinalIgnoreCase))
        {
            vm.IsCompatible = false;
            vm.CompatibilityMessage = "Socket CPU và Mainboard chưa tương thích.";
        }

        if (ram != null && mainboard != null && !string.IsNullOrWhiteSpace(ram.RamType) && !string.Equals(ram.RamType, mainboard.RamType, StringComparison.OrdinalIgnoreCase))
        {
            vm.IsCompatible = false;
            vm.CompatibilityMessage = "Loại RAM chưa tương thích với Mainboard.";
        }

        if (vm.IsCompatible) vm.CompatibilityMessage = "Cấu hình đang tương thích cơ bản.";
        return vm;
    }
}
