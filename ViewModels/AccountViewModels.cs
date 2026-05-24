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

public class AccountProfileVm
{
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ChangePasswordVm
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
    [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới")]
    [Compare(nameof(NewPassword), ErrorMessage = "Nhập lại mật khẩu mới không khớp")]
    [DataType(DataType.Password)]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
