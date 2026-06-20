using Datn.PcStore.Data;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Services;

public interface ISearchSuggestionService
{
    Task<SearchSuggestionData> GetHeaderSuggestionsAsync(CancellationToken cancellationToken = default);
}

public sealed record SearchSuggestionData(
    IReadOnlyList<string> HotKeywords,
    IReadOnlyList<string> ProductNames,
    IReadOnlyList<string> CategoryNames,
    IReadOnlyList<string> BrandNames);

public class SearchSuggestionService : ISearchSuggestionService
{
    private static readonly string[] FallbackHotKeywords =
    [
        "RTX 5070 Ti",
        "RTX 5060",
        "Ryzen 7 9800X3D",
        "PC Gaming",
        "PC Văn Phòng"
    ];

    private readonly ApplicationDbContext _db;

    public SearchSuggestionService(ApplicationDbContext db) => _db = db;

    public async Task<SearchSuggestionData> GetHeaderSuggestionsAsync(CancellationToken cancellationToken = default)
    {
        var hotKeywords = await GetHotKeywordsAsync(cancellationToken);
        var productNames = await _db.Products.AsNoTracking()
            .Where(product => product.IsActive)
            .OrderByDescending(product => product.IsHotSale || product.IsDailyDeal || product.IsPromotion)
            .ThenByDescending(product => product.StockQuantity)
            .ThenBy(product => product.Name)
            .Select(product => product.Name)
            .Where(name => name != string.Empty)
            .Take(80)
            .ToListAsync(cancellationToken);

        var categoryNames = await _db.Categories.AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => category.Name)
            .Where(name => name != string.Empty)
            .Take(30)
            .ToListAsync(cancellationToken);

        var brandNames = await _db.Products.AsNoTracking()
            .Where(product => product.IsActive && product.Brand != null && product.Brand != string.Empty && product.Brand != "N/A")
            .Select(product => product.Brand!)
            .Distinct()
            .OrderBy(brand => brand)
            .Take(30)
            .ToListAsync(cancellationToken);

        return new SearchSuggestionData(hotKeywords, productNames, categoryNames, brandNames);
    }

    private async Task<IReadOnlyList<string>> GetHotKeywordsAsync(CancellationToken cancellationToken)
    {
        var productKeywords = await _db.Products.AsNoTracking()
            .Where(product => product.IsActive && (product.IsHotSale || product.IsDailyDeal || product.IsPromotion))
            .OrderByDescending(product => product.IsHotSale)
            .ThenByDescending(product => product.IsDailyDeal)
            .ThenByDescending(product => product.IsPromotion)
            .ThenByDescending(product => product.StockQuantity)
            .Select(product => product.Name)
            .Where(name => name != string.Empty)
            .Take(5)
            .ToListAsync(cancellationToken);

        if (productKeywords.Count > 0)
        {
            return productKeywords;
        }

        var categoryKeywords = await _db.Categories.AsNoTracking()
            .Where(category => category.Name.Contains("PC") || category.Name.Contains("Gaming"))
            .OrderBy(category => category.Name)
            .Select(category => category.Name)
            .Take(5)
            .ToListAsync(cancellationToken);

        return categoryKeywords.Count > 0 ? categoryKeywords : FallbackHotKeywords;
    }
}
