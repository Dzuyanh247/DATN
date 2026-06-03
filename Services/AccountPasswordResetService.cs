using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Datn.PcStore.Services;

public class AccountPasswordResetService : IAccountPasswordResetService
{
    private readonly ApplicationDbContext _db;
    private readonly IDataProtector _protector;
    private readonly PasswordHasher<User> _passwordHasher = new();
    private readonly IdentityOptions _identityOptions;

    public AccountPasswordResetService(
        ApplicationDbContext db,
        IDataProtectionProvider dataProtectionProvider,
        IOptions<IdentityOptions> identityOptions)
    {
        _db = db;
        _protector = dataProtectionProvider.CreateProtector("Datn.PcStore.PasswordResetToken.v1");
        _identityOptions = identityOptions.Value;
    }

    public Task<string> GeneratePasswordResetTokenAsync(User user)
    {
        var payload = $"{user.Id}|{user.PasswordHash}|{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}|{Guid.NewGuid():N}";
        return Task.FromResult(_protector.Protect(payload));
    }

    public async Task<IdentityResult> ResetPasswordAsync(User user, string token, string newPassword)
    {
        if (!IsTokenValid(user, token))
        {
            return IdentityResult.Failed(new IdentityError { Description = "Phiên đặt lại mật khẩu không hợp lệ." });
        }

        var passwordErrors = ValidatePassword(newPassword).ToArray();
        if (passwordErrors.Length > 0)
        {
            return IdentityResult.Failed(passwordErrors);
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        await _db.SaveChangesAsync();
        return IdentityResult.Success;
    }

    private bool IsTokenValid(User user, string token)
    {
        try
        {
            var payload = _protector.Unprotect(token).Split('|');
            if (payload.Length != 4) return false;
            if (!int.TryParse(payload[0], out var userId) || userId != user.Id) return false;
            if (!string.Equals(payload[1], user.PasswordHash, StringComparison.Ordinal)) return false;
            if (!long.TryParse(payload[2], out var issuedAtUnix)) return false;

            var issuedAt = DateTimeOffset.FromUnixTimeSeconds(issuedAtUnix);
            return DateTimeOffset.UtcNow - issuedAt <= TimeSpan.FromHours(1);
        }
        catch
        {
            return false;
        }
    }

    private IEnumerable<IdentityError> ValidatePassword(string password)
    {
        var options = _identityOptions.Password;

        if (string.IsNullOrWhiteSpace(password) || password.Length < options.RequiredLength)
        {
            yield return new IdentityError { Description = $"Mật khẩu phải có ít nhất {options.RequiredLength} ký tự." };
        }

        if (options.RequireDigit && !password.Any(char.IsDigit))
        {
            yield return new IdentityError { Description = "Mật khẩu phải có ít nhất một chữ số." };
        }

        if (options.RequireLowercase && !password.Any(char.IsLower))
        {
            yield return new IdentityError { Description = "Mật khẩu phải có ít nhất một chữ thường." };
        }

        if (options.RequireUppercase && !password.Any(char.IsUpper))
        {
            yield return new IdentityError { Description = "Mật khẩu phải có ít nhất một chữ hoa." };
        }

        if (options.RequireNonAlphanumeric && password.All(char.IsLetterOrDigit))
        {
            yield return new IdentityError { Description = "Mật khẩu phải có ít nhất một ký tự đặc biệt." };
        }

        if (options.RequiredUniqueChars > 1 && password.Distinct().Count() < options.RequiredUniqueChars)
        {
            yield return new IdentityError { Description = $"Mật khẩu phải có ít nhất {options.RequiredUniqueChars} ký tự khác nhau." };
        }
    }
}
