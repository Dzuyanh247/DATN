using System.Security.Claims;
using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Datn.PcStore.Services;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
public class AdminUsersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IAuthService _authService;

    public AdminUsersController(ApplicationDbContext db, IAuthService authService)
    {
        _db = db;
        _authService = authService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? keyword)
    {
        var query = _db.Users.Include(x => x.Role).AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => (x.FullName ?? string.Empty).Contains(keyword) || (x.Email ?? string.Empty).Contains(keyword) || (x.Username ?? string.Empty).Contains(keyword));
        }

        var vm = new AdminUserIndexVm
        {
            Keyword = keyword,
            Users = await query.OrderByDescending(x => x.CreatedAt)
                .Select(x => new AdminUserListItemVm
                {
                    Id = x.Id,
                    Username = x.Username ?? string.Empty,
                    FullName = x.FullName ?? string.Empty,
                    Email = x.Email ?? string.Empty,
                    Role = x.Role != null ? x.Role.Name : string.Empty,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt
                }).ToListAsync()
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(await BuildVmAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminUserUpsertVm vm)
    {
        await LoadRolesAsync(vm);

        if (string.IsNullOrWhiteSpace(vm.Password))
        {
            ModelState.AddModelError(nameof(vm.Password), "Mật khẩu là bắt buộc khi tạo tài khoản.");
        }

        if (await _db.Users.AnyAsync(x => x.Email == vm.Email))
        {
            ModelState.AddModelError(nameof(vm.Email), "Email đã tồn tại.");
        }

        if (await _db.Users.AnyAsync(x => x.Username == vm.Username))
        {
            ModelState.AddModelError(nameof(vm.Username), "Username đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var user = new User
        {
            Username = vm.Username,
            FullName = vm.FullName,
            Email = vm.Email,
            Phone = vm.Phone,
            Address = vm.Address,
            RoleId = vm.RoleId,
            IsActive = vm.IsActive,
            PasswordHash = _authService.HashPassword(vm.Password!)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Đã tạo tài khoản mới.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vm = await BuildVmAsync(id);
        if (vm == null)
        {
            return NotFound();
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminUserUpsertVm vm)
    {
        await LoadRolesAsync(vm);

        if (await _db.Users.AnyAsync(x => x.Id != vm.Id && x.Email == vm.Email))
        {
            ModelState.AddModelError(nameof(vm.Email), "Email đã tồn tại.");
        }

        if (await _db.Users.AnyAsync(x => x.Id != vm.Id && x.Username == vm.Username))
        {
            ModelState.AddModelError(nameof(vm.Username), "Username đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var user = await _db.Users.FindAsync(vm.Id);
        if (user == null)
        {
            return NotFound();
        }

        user.Username = vm.Username;
        user.FullName = vm.FullName;
        user.Email = vm.Email;
        user.Phone = vm.Phone;
        user.Address = vm.Address;
        user.RoleId = vm.RoleId;
        user.IsActive = vm.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(vm.Password))
        {
            user.PasswordHash = _authService.HashPassword(vm.Password);
        }

        await _db.SaveChangesAsync();
        TempData["Ok"] = "Đã cập nhật tài khoản.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == id.ToString())
        {
            TempData["Err"] = "Bạn không thể tự khóa tài khoản của mình.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _db.Users.FindAsync(id);
        if (user == null)
        {
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Ok"] = user.IsActive ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == id.ToString())
        {
            TempData["Err"] = "Bạn không thể tự xóa chính mình.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _db.Users.FindAsync(id);
        if (user == null)
        {
            return RedirectToAction(nameof(Index));
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Đã xóa tài khoản.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<AdminUserUpsertVm?> BuildVmAsync(int? userId = null)
    {
        var vm = new AdminUserUpsertVm();
        await LoadRolesAsync(vm);

        if (!userId.HasValue)
        {
            return vm;
        }

        var user = await _db.Users.FindAsync(userId.Value);
        if (user == null)
        {
            return null;
        }

        vm.Id = user.Id;
        vm.Username = user.Username ?? string.Empty;
        vm.FullName = user.FullName ?? string.Empty;
        vm.Email = user.Email ?? string.Empty;
        vm.Phone = user.Phone ?? string.Empty;
        vm.Address = user.Address ?? string.Empty;
        vm.RoleId = user.RoleId;
        vm.IsActive = user.IsActive;

        return vm;
    }

    private async Task LoadRolesAsync(AdminUserUpsertVm vm)
    {
        vm.Roles = await _db.Roles.OrderBy(x => x.Name)
            .Select(x => new RoleOptionVm
            {
                Id = x.Id,
                Name = x.Name ?? string.Empty
            }).ToListAsync();
    }
}
