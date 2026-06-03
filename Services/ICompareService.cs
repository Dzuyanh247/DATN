using Datn.PcStore.Models;

namespace Datn.PcStore.Services;

public interface ICompareService
{
    IReadOnlyList<int> GetIds();
    Task<IReadOnlyList<Product>> GetProductsAsync();
    bool Add(int productId);
    bool Remove(int productId);
    void Clear();
    bool Contains(int productId);
}
