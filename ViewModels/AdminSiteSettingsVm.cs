using Microsoft.AspNetCore.Http;

namespace Datn.PcStore.ViewModels;

public class AdminSiteSettingsVm
{
    public string SiteName { get; set; } = "KKSHOP";
    public string? LogoUrl { get; set; }
    public IFormFile? LogoFile { get; set; }
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}
