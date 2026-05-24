using System.Security.Claims;
using Datn.PcStore.Constants;
using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Datn.PcStore.Services;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
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

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null) return NotFound();

        var vm = new AccountSettingsViewModel
        {
            Profile = new AccountProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.Phone ?? string.Empty
            },
            Username = user.Username,
            Address = string.IsNullOrWhiteSpace(user.Address) ? null : user.Address,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };

        return View(vm);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile([Bind(Prefix = "Profile")] AccountProfileViewModel vm)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Không tìm thấy tài khoản người dùng.");
            return RedirectToAction(nameof(Profile));
        }

        if (!string.IsNullOrWhiteSpace(vm.PhoneNumber) && !System.Text.RegularExpressions.Regex.IsMatch(vm.PhoneNumber, "^(0|\\+84)[0-9]{9,10}$"))
        {
            ModelState.AddModelError(nameof(vm.PhoneNumber), "Số điện thoại không hợp lệ.");
        }

        if (!ModelState.IsValid)
        {
            return View("Profile", await BuildProfileVmAsync(user, vm));
        }

        var normalizedEmail = vm.Email.Trim();
        var existingByEmail = await _db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail && x.Id != userId);
        if (existingByEmail != null)
        {
            ModelState.AddModelError(nameof(vm.Email), "Email đã được sử dụng bởi tài khoản khác.");
            return View("Profile", await BuildProfileVmAsync(user, vm));
        }

        user.FullName = vm.FullName.Trim();
        user.Phone = vm.PhoneNumber.Trim();
        user.Email = normalizedEmail;
        user.Username = normalizedEmail;
        await _db.SaveChangesAsync();
        await RefreshAuthClaimsAsync(user);

        TempData["ProfileMessage"] = "Cập nhật thông tin thành công";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "ChangePassword")] ChangePasswordViewModel vm)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy tài khoản người dùng.";
            return RedirectToAction(nameof(Profile));
        }

        if (!ModelState.IsValid)
        {
            var profileVm = await BuildProfileVmAsync(user, changePasswordVm: vm);
            return View("Profile", profileVm);
        }

        if (!_authService.VerifyPassword(vm.CurrentPassword, user.PasswordHash))
        {
            ModelState.AddModelError(nameof(vm.CurrentPassword), "Mật khẩu hiện tại không đúng.");
            var profileVm = await BuildProfileVmAsync(user, changePasswordVm: vm);
            return View("Profile", profileVm);
        }

        user.PasswordHash = _authService.HashPassword(vm.NewPassword);
        await _db.SaveChangesAsync();
        await RefreshAuthClaimsAsync(user);
        TempData["PasswordMessage"] = "Đổi mật khẩu thành công.";
        return RedirectToAction(nameof(Profile));
    }

    private async Task<AccountSettingsViewModel> BuildProfileVmAsync(User user, AccountProfileViewModel? profileVm = null, ChangePasswordViewModel? changePasswordVm = null)
    {
        await Task.CompletedTask;
        return new AccountSettingsViewModel
        {
            Profile = profileVm ?? new AccountProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.Phone ?? string.Empty
            },
            Username = user.Username,
            Address = string.IsNullOrWhiteSpace(user.Address) ? null : user.Address,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            ChangePassword = changePasswordVm ?? new ChangePasswordViewModel()
        };
    }

    private async Task RefreshAuthClaimsAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new("username", user.Username),
            new(ClaimTypes.Role, user.Role?.Name ?? "Customer")
        };
        var identity = new ClaimsIdentity(claims, AuthSchemes.PcStoreCookie);
        await HttpContext.SignInAsync(AuthSchemes.PcStoreCookie, new ClaimsPrincipal(identity));
    }
}
