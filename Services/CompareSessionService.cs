using System.Text.Json;
using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Services;

public class CompareSessionService : ICompareService
{
    public const string SessionKey = "CompareProductIds";
    public const int MaxCompareProducts = 2;

    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CompareSessionService(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public IReadOnlyList<int> GetIds() => ReadIds();

    public async Task<IReadOnlyList<Product>> GetProductsAsync()
    {
        var ids = ReadIds();
        if (ids.Count == 0) return Array.Empty<Product>();

        var products = await _db.Products
            .Include(p => p.ProductImages.OrderBy(x => x.SortOrder))
            .Where(p => ids.Contains(p.Id) && p.IsActive)
            .ToListAsync();

        return ids
            .Select(id => products.FirstOrDefault(p => p.Id == id))
            .Where(p => p != null)
            .Cast<Product>()
            .ToList();
    }

    public bool Add(int productId)
    {
        var ids = ReadIds();
        if (ids.Contains(productId) || ids.Count >= MaxCompareProducts)
        {
            return false;
        }

        ids.Add(productId);
        SaveIds(ids);
        return true;
    }

    public bool Remove(int productId)
    {
        var ids = ReadIds();
        var removed = ids.Remove(productId);
        if (removed)
        {
            SaveIds(ids);
        }

        return removed;
    }

    public void Clear() => Session.Remove(SessionKey);

    public bool Contains(int productId) => ReadIds().Contains(productId);

    private List<int> ReadIds()
    {
        var raw = Session.GetString(SessionKey);
        if (string.IsNullOrWhiteSpace(raw)) return new List<int>();

        try
        {
            return JsonSerializer.Deserialize<List<int>>(raw)?
                .Where(id => id > 0)
                .Distinct()
                .Take(MaxCompareProducts)
                .ToList() ?? new List<int>();
        }
        catch (JsonException)
        {
            return new List<int>();
        }
    }

    private void SaveIds(IEnumerable<int> ids)
    {
        var normalizedIds = ids
            .Where(id => id > 0)
            .Distinct()
            .Take(MaxCompareProducts)
            .ToList();

        if (normalizedIds.Count == 0)
        {
            Session.Remove(SessionKey);
            return;
        }

        Session.SetString(SessionKey, JsonSerializer.Serialize(normalizedIds));
    }

    private ISession Session => _httpContextAccessor.HttpContext?.Session
        ?? throw new InvalidOperationException("Session is not available for compare service.");
}
