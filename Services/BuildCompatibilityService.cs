using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;

namespace Datn.PcStore.Services;

public class BuildCompatibilityService
{
    public bool IsCompatible(IEnumerable<SelectedComponentViewModel> selectedComponents, Product newProduct, out string warning)
    {
        warning = string.Empty;
        var list = selectedComponents.ToList();
        var cpu = list.FirstOrDefault(x => x.Type == "CPU");
        var main = list.FirstOrDefault(x => x.Type == "MAINBOARD");
        var ram = list.FirstOrDefault(x => x.Type == "RAM");

        if (string.Equals(newProduct.ComponentType, "CPU", StringComparison.OrdinalIgnoreCase) && main != null && !string.IsNullOrWhiteSpace(newProduct.CpuSocket))
            warning = "Cần kiểm tra socket CPU/Mainboard trước khi mua.";

        if (!string.IsNullOrWhiteSpace(newProduct.ComponentType) && newProduct.ComponentType.Contains("main", StringComparison.OrdinalIgnoreCase) && cpu != null)
            warning = "Cần kiểm tra socket CPU/Mainboard trước khi mua.";

        if (string.Equals(newProduct.ComponentType, "RAM", StringComparison.OrdinalIgnoreCase) && main != null)
            warning = "Cần kiểm tra chuẩn DDR của RAM/Mainboard.";

        if (!string.IsNullOrWhiteSpace(newProduct.ComponentType) && newProduct.ComponentType.Contains("PSU", StringComparison.OrdinalIgnoreCase))
            warning = "Nên kiểm tra công suất PSU so với VGA/CPU.";

        return true;
    }
}
