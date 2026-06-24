namespace Datn.PcStore.ViewModels;

public class AdminImageConfigRowViewModel
{
    public string FieldName { get; set; } = string.Empty;
    public string InputId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? FallbackUrl { get; set; }
    public string? PreviewClass { get; set; }
    public string? Placeholder { get; set; }
}
