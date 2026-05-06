using System.ComponentModel.DataAnnotations;

namespace Datn.PcStore.ViewModels;

public class AdminSiteSettingsVm
{
    public string SiteName { get; set; } = "KKSHOP";

    [Display(Name = "Logo URL")]
    [MaxLength(1000, ErrorMessage = "Logo URL không được vượt quá 1000 ký tự")]
    public string? LogoUrl { get; set; }

    [Display(Name = "DealSectionBackgroundUrl")]
    [MaxLength(1000)]
    public string? DealSectionBackgroundUrl { get; set; }

    [Display(Name = "HotPromotionBackgroundUrl")]
    [MaxLength(1000)]
    public string? HotPromotionBackgroundUrl { get; set; }

    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}
