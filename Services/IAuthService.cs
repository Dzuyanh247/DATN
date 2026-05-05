using Datn.PcStore.Models;

namespace Datn.PcStore.Services;

public interface IAuthService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
    Task<User?> ValidateUserAsync(string email, string password);
}
