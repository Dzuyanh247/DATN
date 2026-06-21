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
            .Select(product => product.Brand ?? string.Empty)
            .Distinct()
            .OrderBy(brand => brand)
            .Take(30)
            .ToListAsync(cancellationToken);

        return new SearchSuggestionData(hotKeywords, productNames.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x?.Trim() ?? string.Empty).ToList(), categoryNames.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x?.Trim() ?? string.Empty).ToList(), brandNames.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList());
    }

    private async Task<IReadOnlyList<string>> GetHotKeywordsAsync(CancellationToken cancellationToken)
    {
        return await _db.SearchKeywords.AsNoTracking()
            .Where(keyword => keyword.IsVisible)
            .OrderByDescending(keyword => keyword.IsPinned)
            .ThenByDescending(keyword => keyword.SearchCount)
            .ThenByDescending(keyword => keyword.LastSearchedAt)
            .Select(keyword => keyword.Keyword)
            .Take(8)
            .ToListAsync(cancellationToken);
    }
}
