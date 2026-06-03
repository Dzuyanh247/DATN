using Datn.PcStore.Models;
using Microsoft.AspNetCore.Identity;

namespace Datn.PcStore.Services;

public interface IAccountPasswordResetService
{
    Task<string> GeneratePasswordResetTokenAsync(User user);
    Task<IdentityResult> ResetPasswordAsync(User user, string token, string newPassword);
}
