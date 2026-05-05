using Datn.PcStore.Data;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
public class AdminDashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    public AdminDashboardController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var vm = new AdminDashboardVm
        {
            ProductCount = await _db.Products.CountAsync(),
            OrderCount = await _db.Orders.CountAsync(),
            UserCount = await _db.Users.CountAsync(),
            WarrantyRequestCount = await _db.WarrantyRequests.CountAsync()
        };

        return View(vm);
    }
}
