using System.ComponentModel.DataAnnotations;

namespace Datn.PcStore.ViewModels;

public class AdminUserListItemVm
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminUserIndexVm
{
    public string? Keyword { get; set; }
    public List<AdminUserListItemVm> Users { get; set; } = new();
}

public class AdminUserUpsertVm
{
    public int Id { get; set; }

    [Required]
    [MaxLength(60)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(120)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(250)]
    public string Address { get; set; } = string.Empty;

    [Required]
    public int RoleId { get; set; }

    public bool IsActive { get; set; } = true;

    [MinLength(6)]
    public string? Password { get; set; }

    public List<RoleOptionVm> Roles { get; set; } = new();
}

public class RoleOptionVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
