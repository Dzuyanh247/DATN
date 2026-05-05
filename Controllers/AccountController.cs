using System.Security.Claims;
using Datn.PcStore.Constants;
using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Datn.PcStore.Services;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IAuthService _authService;
    private readonly ICartService _cartService;

    public AccountController(ApplicationDbContext db, IAuthService authService, ICartService cartService)
    {
        _db = db;
        _authService = authService;
        _cartService = cartService;
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (await _db.Users.AnyAsync(u => u.Email == vm.Email))
        {
            ModelState.AddModelError("Email", "Email đã tồn tại");
            return View(vm);
        }

        var customerRoleId = await _db.Roles.Where(r => r.Name == "Customer").Select(r => r.Id).FirstAsync();
        var username = vm.Email.Split('@')[0];
        if (await _db.Users.AnyAsync(x => x.Username == username))
        {
            username = $"{username}{DateTime.UtcNow:ddHHmmss}";
        }

        var user = new User
        {
            Username = username,
            FullName = vm.FullName,
            Email = vm.Email,
            PasswordHash = _authService.HashPassword(vm.Password),
            Phone = vm.Phone,
            Address = vm.Address,
            RoleId = customerRoleId,
            IsActive = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Đăng ký thành công, vui lòng đăng nhập.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await _authService.ValidateUserAsync(vm.Email, vm.Password);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Sai email hoặc mật khẩu");
            return View(vm);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa.");
            return View(vm);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new("username", user.Username),
            new(ClaimTypes.Role, user.Role?.Name ?? "Customer")
        };

        var identity = new ClaimsIdentity(claims, AuthSchemes.PcStoreCookie);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(AuthSchemes.PcStoreCookie, principal);
        await _cartService.MergeGuestCartAsync(user.Id);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AuthSchemes.PcStoreCookie);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();
}
