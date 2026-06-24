namespace Datn.PcStore.ViewModels;

public class AdminImageUploaderViewModel
{
    public string? FieldName { get; set; }
    public string? GalleryFieldName { get; set; }
    public string? Label { get; set; }
    public string? HelpText { get; set; }
    public string? Value { get; set; }
    public string? GalleryValue { get; set; }
    public bool Hidden { get; set; } = false;
    public bool Multiple { get; set; } = false;
    public string? AspectHint { get; set; }
    public string? CssClass { get; set; }
}
