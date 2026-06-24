using System.Security.Claims;

namespace Datn.PcStore.Services;

public static class RolePermissionService
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";
    public const string Staff = "Staff";
    public const string CustomerSupport = "CustomerSupport";
    public const string SupportStaff = "SupportStaff";

    public const string BackOfficeRoles = "Admin,Staff,CustomerSupport,SupportStaff";
    public const string ProductManagerRoles = "Admin,Staff";
    public const string OrderManagerRoles = "Admin,Staff,SupportStaff";
    public const string SupportManagerRoles = "Admin,CustomerSupport,SupportStaff";
    public const string AdminOnlyRoles = "Admin";

    private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        Admin, Customer, Staff, CustomerSupport, SupportStaff
    };

    public static bool IsValidRole(string? role) => !string.IsNullOrWhiteSpace(role) && ValidRoles.Contains(role.Trim());
    public static string NormalizeRole(string? role) => IsValidRole(role) ? role!.Trim() : Customer;
    public static bool IsAdminRole(string? role) => string.Equals(NormalizeRole(role), Admin, StringComparison.OrdinalIgnoreCase);
    public static bool IsBackOfficeRole(string? role) => NormalizeRole(role) is Admin or Staff or CustomerSupport or SupportStaff;
    public static bool CanManageProducts(string? role) => NormalizeRole(role) is Admin or Staff;
    public static bool CanManageOrders(string? role) => NormalizeRole(role) is Admin or Staff or SupportStaff;
    public static bool CanManageSupport(string? role) => NormalizeRole(role) is Admin or CustomerSupport or SupportStaff;
    public static bool CanAccessAdminArea(string? role) => IsBackOfficeRole(role);
    public static bool CanManageUsers(string? role) => IsAdminRole(role);
    public static bool CanManageSettings(string? role) => IsAdminRole(role);

    public static bool IsBackOfficeUser(this ClaimsPrincipal user) => IsBackOfficeRole(GetRole(user));
    public static bool CanManageProducts(this ClaimsPrincipal user) => CanManageProducts(GetRole(user));
    public static bool CanManageOrders(this ClaimsPrincipal user) => CanManageOrders(GetRole(user));
    public static bool CanManageSupport(this ClaimsPrincipal user) => CanManageSupport(GetRole(user));
    public static bool CanManageUsers(this ClaimsPrincipal user) => CanManageUsers(GetRole(user));
    public static bool CanManageSettings(this ClaimsPrincipal user) => CanManageSettings(GetRole(user));

    public static string GetRole(ClaimsPrincipal user) => NormalizeRole(user.FindFirstValue(ClaimTypes.Role));

    public static string GetDisplayName(string? role) => NormalizeRole(role) switch
    {
        Admin => "Quản trị viên",
        Staff => "Nhân viên bán hàng",
        CustomerSupport => "Chăm sóc khách hàng",
        SupportStaff => "Nhân viên hỗ trợ",
        _ => "Khách hàng"
    };

    public static string GetDescription(string? role) => NormalizeRole(role) switch
    {
        Admin => "Toàn quyền hệ thống.",
        Staff => "Quản lý sản phẩm và đơn hàng.",
        CustomerSupport => "Xử lý hỗ trợ, đánh giá, liên hệ.",
        SupportStaff => "Hỗ trợ đơn hàng và bảo hành.",
        _ => "Không có quyền quản trị."
    };
}
