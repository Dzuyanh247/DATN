using System.Globalization;
using System.Text.RegularExpressions;
using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Datn.PcStore.Services;

public record AiProductContext(
    int Id,
    string Name,
    decimal Price,
    string Specifications,
    string StockStatus,
    string Link,
    string Category,
    int StockQuantity);

public interface IProductSearchForAiService
{
    Task<IReadOnlyList<AiProductContext>> SearchAsync(string message, CancellationToken cancellationToken = default);
}

public partial class ProductSearchForAiService : IProductSearchForAiService
{
    private readonly ApplicationDbContext _db;
    private readonly AiChatOptions _options;

    public ProductSearchForAiService(ApplicationDbContext db, IOptions<AiChatOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<AiProductContext>> SearchAsync(string message, CancellationToken cancellationToken = default)
    {
        message = (message ?? string.Empty).Trim();
        var max = Math.Clamp(_options.MaxProductsContext, 1, 10);
        var budget = ParseBudget(message);
        var tokens = ExtractTokens(message);
        var needsPc = IsPcQuestion(message);

        var baseQuery = _db.Products.AsNoTracking().Include(x => x.Category)
            .Where(x => x.IsActive && (x.IsInStock || x.StockQuantity > 0));

        if (needsPc)
            baseQuery = baseQuery.Where(x => x.ProductType == ProductKinds.PC || (x.Category != null && x.Category.Name.Contains("PC")) || x.Name.Contains("PC"));

        if (budget.HasValue)
            baseQuery = baseQuery.Where(x => (x.DiscountPrice ?? x.SalePrice ?? x.Price) <= budget.Value);

        if (tokens.Count > 0)
        {
            var query = baseQuery;
            foreach (var token in tokens.Take(8))
            {
                var like = $"%{token}%";
                query = query.Where(x => EF.Functions.Like(x.Name, like)
                    || (x.ShortDescription != null && EF.Functions.Like(x.ShortDescription, like))
                    || (x.Description != null && EF.Functions.Like(x.Description, like))
                    || (x.Specifications != null && EF.Functions.Like(x.Specifications, like))
                    || (x.Category != null && EF.Functions.Like(x.Category.Name, like)));
            }

            var strict = await Project(query.OrderBy(x => x.DiscountPrice ?? x.SalePrice ?? x.Price).Take(max), cancellationToken);
            if (strict.Count > 0) return strict;

            var sample = await baseQuery.OrderBy(x => x.DiscountPrice ?? x.SalePrice ?? x.Price).Take(60).ToListAsync(cancellationToken);
            var looseRows = sample.Where(x => tokens.Take(10).Any(t => ContainsIgnoreCase(x.Name, t)
                || ContainsIgnoreCase(x.ShortDescription, t)
                || ContainsIgnoreCase(x.Description, t)
                || ContainsIgnoreCase(x.Specifications, t)))
                .Take(max)
                .Select(x => new AiProductContext(
                    x.Id, x.Name, x.DiscountPrice ?? x.SalePrice ?? x.Price,
                    TrimText(string.IsNullOrWhiteSpace(x.Specifications) ? x.ShortDescription ?? string.Empty : x.Specifications!, 450),
                    x.StockQuantity > 0 ? $"Còn hàng ({x.StockQuantity})" : "Tạm hết hàng",
                    $"/Products/Detail/{x.Id}", x.Category?.Name ?? "Chưa phân loại", x.StockQuantity))
                .ToList();
            if (looseRows.Count > 0) return looseRows;
        }

        return await Project(baseQuery.OrderBy(x => x.DiscountPrice ?? x.SalePrice ?? x.Price).Take(max), cancellationToken);
    }

    private static async Task<List<AiProductContext>> Project(IQueryable<Product> query, CancellationToken ct)
    {
        var rows = await query.Select(x => new
        {
            x.Id, x.Name, Price = x.DiscountPrice ?? x.SalePrice ?? x.Price, x.Specifications, x.ShortDescription, x.StockQuantity, x.Slug,
            Category = x.Category != null ? x.Category.Name : "Chưa phân loại"
        }).ToListAsync(ct);
        return rows.Select(x => new AiProductContext(
            x.Id,
            x.Name,
            x.Price,
            TrimText(string.IsNullOrWhiteSpace(x.Specifications) ? x.ShortDescription ?? string.Empty : x.Specifications!, 450),
            x.StockQuantity > 0 ? $"Còn hàng ({x.StockQuantity})" : "Tạm hết hàng",
            $"/Products/Detail/{x.Id}",
            x.Category,
            x.StockQuantity)).ToList();
    }

    private static decimal? ParseBudget(string text)
    {
        var match = BudgetRegex().Match(text.ToLowerInvariant());
        if (!match.Success) return null;
        if (!decimal.TryParse(match.Groups[1].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)) return null;
        return amount < 1000 ? amount * 1_000_000 : amount;
    }

    private static List<string> ExtractTokens(string text)
    {
        var lower = text.ToLowerInvariant();
        var tokens = new List<string>();
        foreach (Match match in HardwareRegex().Matches(text)) tokens.Add(match.Value);
        foreach (var word in new[] { "valorant", "gaming", "game", "văn phòng", "do hoa", "đồ họa", "livestream", "rtx", "rx", "ryzen", "intel" })
            if (lower.Contains(word)) tokens.Add(word);
        return tokens.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool ContainsIgnoreCase(string? source, string value) => !string.IsNullOrWhiteSpace(source) && source.Contains(value, StringComparison.OrdinalIgnoreCase);

    private static bool IsPcQuestion(string text) => text.Contains("pc", StringComparison.OrdinalIgnoreCase) || text.Contains("máy tính", StringComparison.OrdinalIgnoreCase) || text.Contains("cấu hình", StringComparison.OrdinalIgnoreCase);
    private static string TrimText(string value, int max) => string.IsNullOrWhiteSpace(value) ? "Đang cập nhật" : value.Length <= max ? value : value[..max] + "...";

    [GeneratedRegex(@"(?:dưới|duoi|khoảng|tam|tầm|<=|<)?\s*(\d+(?:[\.,]\d+)?)\s*(?:triệu|tr|m|million)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BudgetRegex();

    [GeneratedRegex(@"\b(?:rtx\s*\d{4}|rx\s*\d{4}|gtx\s*\d{4}|i[3579](?:-\d{4,5}[a-z]*)?|ryzen\s*[3579](?:\s*\d{4}[a-z]*)?)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HardwareRegex();
}
