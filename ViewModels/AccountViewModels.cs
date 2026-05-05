using System.ComponentModel.DataAnnotations;

namespace Datn.PcStore.ViewModels;

public class RegisterVm
{
    [Required] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(6)] public string Password { get; set; } = string.Empty;
    [Required] public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class LoginVm
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}
