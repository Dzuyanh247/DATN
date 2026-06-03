using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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
    private readonly IEmailSender _emailSender;
    private readonly IAccountPasswordResetService _passwordResetService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        ApplicationDbContext db,
        IAuthService authService,
        ICartService cartService,
        IEmailSender emailSender,
        IAccountPasswordResetService passwordResetService,
        ILogger<AccountController> logger)
    {
        _db = db;
        _authService = authService;
        _cartService = cartService;
        _emailSender = emailSender;
        _passwordResetService = passwordResetService;
        _logger = logger;
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

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var normalizedEmail = vm.Email.Trim();
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail);
        if (user != null)
        {
            var now = DateTime.UtcNow;
            var activeOtps = await _db.PasswordResetOtps
                .Where(x => x.UserId == user.Id && !x.IsUsed)
                .ToListAsync();

            foreach (var oldOtp in activeOtps)
            {
                oldOtp.IsUsed = true;
                oldOtp.UsedAt = now;
            }

            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            _db.PasswordResetOtps.Add(new PasswordResetOtp
            {
                UserId = user.Id,
                Email = normalizedEmail,
                CodeHash = HashOtp(normalizedEmail, code),
                ExpiresAt = now.AddMinutes(10),
                IsUsed = false,
                CreatedAt = now
            });
            await _db.SaveChangesAsync();

            var plainTextMessage = $"Mã xác nhận đặt lại mật khẩu KKSHOP của bạn là {code}. Mã có hiệu lực 10 phút.";
            var htmlMessage = $"<p>Xin chào {System.Net.WebUtility.HtmlEncode(user.FullName)},</p>"
                + $"<p>Mã xác nhận đặt lại mật khẩu KKSHOP của bạn là <strong>{code}</strong>.</p>"
                + "<p>Mã có hiệu lực 10 phút. Vui lòng không chia sẻ mã này với bất kỳ ai.</p>";

            try
            {
                await _emailSender.SendEmailAsync(
                    normalizedEmail,
                    "Mã xác nhận đặt lại mật khẩu KKSHOP",
                    htmlMessage,
                    plainTextMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể gửi OTP đặt lại mật khẩu tới {Email}.", normalizedEmail);
            }
        }

        TempData["Ok"] = "Nếu email tồn tại trong hệ thống, mã xác nhận đã được gửi.";
        TempData["ResetEmail"] = normalizedEmail;
        return RedirectToAction(nameof(VerifyResetCode));
    }

    [HttpGet]
    public IActionResult VerifyResetCode()
    {
        return View(new VerifyResetCodeViewModel
        {
            Email = TempData.Peek("ResetEmail") as string ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyResetCode(VerifyResetCodeViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var normalizedEmail = vm.Email.Trim();
        var codeHash = HashOtp(normalizedEmail, vm.Code.Trim());
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail);
        if (user == null)
        {
            ModelState.AddModelError(nameof(vm.Code), "Mã xác nhận không hợp lệ.");
            return View(vm);
        }

        var otp = await _db.PasswordResetOtps
            .Where(x => x.UserId == user.Id && x.CodeHash == codeHash)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (otp == null)
        {
            ModelState.AddModelError(nameof(vm.Code), "Mã xác nhận không hợp lệ. Vui lòng kiểm tra và thử lại.");
            return View(vm);
        }

        if (otp.IsUsed)
        {
            ModelState.AddModelError(nameof(vm.Code), "Mã xác nhận đã được sử dụng. Vui lòng yêu cầu mã mới.");
            return View(vm);
        }

        if (otp.ExpiresAt < DateTime.UtcNow)
        {
            ModelState.AddModelError(nameof(vm.Code), "Mã xác nhận đã hết hạn");
            return View(vm);
        }

        var token = await _passwordResetService.GeneratePasswordResetTokenAsync(user);
        var resetResult = await _passwordResetService.ResetPasswordAsync(user, token, vm.NewPassword);
        if (!resetResult.Succeeded)
        {
            foreach (var error in resetResult.Errors)
            {
                ModelState.AddModelError(nameof(vm.NewPassword), error.Description);
            }

            return View(vm);
        }

        otp.IsUsed = true;
        otp.UsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Đổi mật khẩu thành công, vui lòng đăng nhập lại.";
        return RedirectToAction(nameof(Login));
    }

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
                PhoneNumber = user.Phone ?? string.Empty,
                Address = user.Address ?? string.Empty
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
            TempData["ErrorMessage"] = "Không tìm thấy tài khoản người dùng.";
            return RedirectToAction(nameof(Profile));
        }

        if (!string.IsNullOrWhiteSpace(vm.PhoneNumber) && !System.Text.RegularExpressions.Regex.IsMatch(vm.PhoneNumber, "^(0|\\+84)[0-9]{9,10}$"))
        {
            ModelState.AddModelError("Profile.PhoneNumber", "Số điện thoại không hợp lệ.");
        }

        if (!ModelState.IsValid)
        {
            return View("Profile", await BuildProfileVmAsync(user, vm));
        }

        var normalizedEmail = vm.Email.Trim();
        var existingByEmail = await _db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail && x.Id != userId);
        if (existingByEmail != null)
        {
            ModelState.AddModelError("Profile.Email", "Email đã được sử dụng bởi tài khoản khác.");
            return View("Profile", await BuildProfileVmAsync(user, vm));
        }

        user.FullName = vm.FullName.Trim();
        user.Phone = vm.PhoneNumber.Trim();
        user.Email = normalizedEmail;
        user.Address = vm.Address?.Trim() ?? string.Empty;
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
                PhoneNumber = user.Phone ?? string.Empty,
                Address = user.Address ?? string.Empty
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
    private static string HashOtp(string email, string code)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{normalizedEmail}:{code}:KKSHOP_PASSWORD_RESET_OTP"));
        return Convert.ToHexString(bytes);
    }

}
