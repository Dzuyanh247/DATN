using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Services;

public interface ISearchKeywordService
{
    string NormalizeKeyword(string? keyword);
    Task TrackSearchAsync(string? keyword, CancellationToken cancellationToken = default);
}

public class SearchKeywordService : ISearchKeywordService
{
    public const int MaxKeywordLength = 120;
    private readonly ApplicationDbContext _db;

    public SearchKeywordService(ApplicationDbContext db) => _db = db;

    public string NormalizeKeyword(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return string.Empty;
        var normalized = string.Join(' ', keyword.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
        return normalized.Length <= MaxKeywordLength ? normalized : normalized[..MaxKeywordLength];
    }

    public async Task TrackSearchAsync(string? keyword, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeKeyword(keyword);
        if (string.IsNullOrWhiteSpace(normalized)) return;

        var now = DateTime.UtcNow;
        var stat = await _db.SearchKeywords.FirstOrDefaultAsync(x => x.Keyword == normalized, cancellationToken);
        if (stat is null)
        {
            _db.SearchKeywords.Add(new SearchKeyword
            {
                Keyword = normalized,
                SearchCount = 1,
                LastSearchedAt = now,
                IsVisible = true,
                IsPinned = false
            });
        }
        else
        {
            stat.SearchCount += 1;
            stat.LastSearchedAt = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
