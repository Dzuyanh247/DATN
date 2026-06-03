using System.Security.Cryptography;
using System.Text;
using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthService(ApplicationDbContext db) => _db = db;

    public string HashPassword(string password)
    {
        // Demo học tập: SHA256 + salt đơn giản, dễ đọc. Có thể thay bằng ASP.NET Identity khi mở rộng.
        var salt = "pcstore-demo-salt";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password + salt));
        return Convert.ToHexString(bytes);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash)) return false;

        if (passwordHash.StartsWith("AQAAAA", StringComparison.Ordinal))
        {
            var result = _passwordHasher.VerifyHashedPassword(new User(), passwordHash, password);
            return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
        }

        return HashPassword(password) == passwordHash;
    }

    public async Task<User?> ValidateUserAsync(string email, string password)
    {
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return null;

        if (user.PasswordHash.StartsWith("AQAAAA", StringComparison.Ordinal))
        {
            var identityResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            return identityResult is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded ? user : null;
        }

        return VerifyPassword(password, user.PasswordHash) ? user : null;
    }
}
