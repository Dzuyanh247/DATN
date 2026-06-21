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
    int StockQuantity,
    string CategoryScope = "COMPONENT",
    string Description = "",
    string Warranty = "");

public interface IProductSearchForAiService
{
    Task<IReadOnlyList<AiProductContext>> SearchAsync(string message, CancellationToken cancellationToken = default);
}

public partial class ProductSearchForAiService : IProductSearchForAiService
{
    private readonly ApplicationDbContext _db;
    private readonly AiChatOptions _options;
    private readonly ILogger<ProductSearchForAiService> _logger;

    public ProductSearchForAiService(ApplicationDbContext db, IOptions<AiChatOptions> options, ILogger<ProductSearchForAiService> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AiProductContext>> SearchAsync(string message, CancellationToken cancellationToken = default)
    {
        message = (message ?? string.Empty).Trim();
        var max = Math.Clamp(_options.MaxProductsContext, 1, 10);
        var analysis = Analyze(message);
        var tokens = ExtractTokens(message, analysis);

        var baseQuery = _db.Products.AsNoTracking().Include(x => x.Category)
            .Where(x => x.IsActive && (x.IsInStock || x.StockQuantity > 0));

        baseQuery = analysis.CategoryScope == "PC"
            ? baseQuery.Where(x => PcCategoryNames.Contains(x.Category!.Name) || x.ProductType == ProductKinds.PC && x.ComponentType == "PC")
            : ApplyComponentScope(baseQuery, analysis.ComponentType);

        if (analysis.Budget.HasValue)
            baseQuery = baseQuery.Where(x => (x.DiscountPrice ?? x.SalePrice ?? x.Price) <= analysis.Budget.Value);

        List<AiProductContext> products;
        if (tokens.Count > 0)
        {
            var sample = await baseQuery.OrderBy(x => x.DiscountPrice ?? x.SalePrice ?? x.Price).Take(120).ToListAsync(cancellationToken);
            var minScore = analysis.HasSpecificProductName && tokens.Count > 1 ? 2 : 1;
            products = sample
                .Select(x => new { Product = x, Score = ScoreProduct(x, tokens, analysis) })
                .Where(x => x.Score >= minScore || (!analysis.HasSpecificProductName && analysis.CategoryScope == "PC"))
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Product.DiscountPrice ?? x.Product.SalePrice ?? x.Product.Price)
                .Take(max)
                .Select(x => ToContext(x.Product, analysis.CategoryScope))
                .ToList();
        }
        else
        {
            products = await Project(baseQuery.OrderBy(x => x.DiscountPrice ?? x.SalePrice ?? x.Price).Take(max), analysis.CategoryScope, cancellationToken);
        }

        if (products.Count == 0 && analysis.CategoryScope == "PC" && analysis.Budget.HasValue)
            products = await Project(_db.Products.AsNoTracking().Include(x => x.Category)
                .Where(x => x.IsActive && (x.IsInStock || x.StockQuantity > 0) && (PcCategoryNames.Contains(x.Category!.Name) || x.ProductType == ProductKinds.PC && x.ComponentType == "PC"))
                .OrderBy(x => x.DiscountPrice ?? x.SalePrice ?? x.Price).Take(max), analysis.CategoryScope, cancellationToken);

        LogSearch(message, analysis, products);
        return products;
    }

    private static async Task<List<AiProductContext>> Project(IQueryable<Product> query, string scope, CancellationToken ct)
    {
        var rows = await query.Select(x => new
        {
            x.Id, x.Name, Price = x.DiscountPrice ?? x.SalePrice ?? x.Price, x.Specifications, x.ShortDescription, x.Description, x.WarrantyDuration, x.WarrantyMonths, x.StockQuantity, x.Slug,
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
            x.StockQuantity,
            scope, TrimText(x.Description ?? string.Empty, 300), !string.IsNullOrWhiteSpace(x.WarrantyDuration) ? x.WarrantyDuration : $"{x.WarrantyMonths} tháng")).ToList();
    }


    private static IQueryable<Product> ApplyComponentScope(IQueryable<Product> query, string? componentType)
    {
        query = query.Where(x => x.ProductType == ProductKinds.Component || x.Category!.Name == "Linh kiện" || x.Category.Name == "Màn hình");
        return string.IsNullOrWhiteSpace(componentType) ? query : query.Where(x => x.ComponentType == componentType);
    }

    private static AiProductContext ToContext(Product x, string scope) => new(
        x.Id, x.Name, x.DiscountPrice ?? x.SalePrice ?? x.Price,
        TrimText(string.IsNullOrWhiteSpace(x.Specifications) ? x.ShortDescription ?? string.Empty : x.Specifications!, 450),
        x.StockQuantity > 0 ? $"Còn hàng ({x.StockQuantity})" : "Tạm hết hàng",
        $"/Products/Detail/{x.Id}", x.Category?.Name ?? "Chưa phân loại", x.StockQuantity, scope,
        TrimText(x.Description ?? string.Empty, 300), !string.IsNullOrWhiteSpace(x.WarrantyDuration) ? x.WarrantyDuration : $"{x.WarrantyMonths} tháng");

    private void LogSearch(string message, AiSearchAnalysis analysis, IReadOnlyList<AiProductContext> products)
    {
        var topProducts = products.Take(3).Select(p => new { p.Id, p.Name, p.Price, p.Category }).ToList();
        _logger.LogInformation("[AI] Message: {Message}; Intent: {Intent}; Scope: {Scope}; Products: {Products}; TopProducts: {TopProducts}; MatchedProduct: {MatchedProduct}; SearchCategory: {SearchCategory}; TargetGame: {TargetGame}; TargetFps: {TargetFps}",
            TrimText(message, 180), analysis.Intent, analysis.CategoryScope, products.Count, string.Join(" | ", products.Take(3).Select(p => p.Name)), products.FirstOrDefault()?.Name ?? "none", analysis.ComponentType ?? analysis.CategoryScope, analysis.TargetGame, analysis.TargetFps);
    }

    private static int ScoreProduct(Product product, IReadOnlyList<string> tokens, AiSearchAnalysis analysis)
    {
        var haystack = $"{product.Name} {product.ShortDescription} {product.Description} {product.Specifications} {product.Category?.Name}";
        var score = tokens.Count(token => ContainsIgnoreCase(haystack, token));
        if (analysis.CategoryScope == "PC" && PcCategoryNames.Contains(product.Category?.Name ?? string.Empty)) score += 5;
        if (!string.IsNullOrWhiteSpace(analysis.TargetGame) && ContainsIgnoreCase(haystack, analysis.TargetGame)) score += 3;
        return score;
    }

    private static AiSearchAnalysis Analyze(string text)
    {
        var normalized = NormalizeText(text);
        var componentType = DetectComponentType(normalized);
        var game = GameWords.FirstOrDefault(g => normalized.Contains(g.Key)).Value;
        var isCompare = ContainsAny(normalized, "so sanh", "compare", "khac nhau");
        var isPcAdvice = (PcIntentWords.Any(normalized.Contains) || !string.IsNullOrWhiteSpace(game)) && componentType == null;
        var isPolicy = ContainsAny(normalized, "tra gop", "freeship", "doi tra", "bao hanh tan noi", "hoa toc", "chinh sach");
        var isOrder = ContainsAny(normalized, "don hang", "ma don", "dh0");
        var isWarranty = ContainsAny(normalized, "bao hanh", "warranty");
        var isPayment = ContainsAny(normalized, "thanh toan", "chuyen khoan", "cod");
        var isHuman = ContainsAny(normalized, "nhan vien", "tu van vien", "nguoi that");
        var intent = isHuman ? "HUMAN_SUPPORT" : isOrder ? "ORDER_QA" : isPayment ? "PAYMENT_QA" : isWarranty ? "WARRANTY_QA" : isPolicy ? "POLICY_QA" : isPcAdvice ? "PC_ADVICE" : isCompare ? "PRODUCT_COMPARE" : "PRODUCT_QA";
        var hasSpecificProductName = DetectSpecificProductName(normalized, componentType);
        return new AiSearchAnalysis(intent, isPcAdvice ? "PC" : "COMPONENT", ParseBudget(text), game, ParseFps(normalized), componentType, hasSpecificProductName);
    }

    private static string? DetectComponentType(string normalized)
    {
        foreach (var item in ComponentIntentWords)
            if (item.Value.Any(normalized.Contains)) return item.Key;
        return null;
    }

    private static int? ParseFps(string normalized)
    {
        var match = FpsRegex().Match(normalized);
        return match.Success && int.TryParse(match.Groups[1].Value, out var fps) ? fps : null;
    }

    private static decimal? ParseBudget(string text)
    {
        var match = BudgetRegex().Match(text.ToLowerInvariant());
        if (!match.Success) return null;
        if (!decimal.TryParse(match.Groups[1].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)) return null;
        return amount < 1000 ? amount * 1_000_000 : amount;
    }

    private static List<string> ExtractTokens(string text, AiSearchAnalysis analysis)
    {
        var lower = text.ToLowerInvariant();
        var tokens = new List<string>();
        foreach (Match match in HardwareRegex().Matches(text)) tokens.Add(match.Value);
        foreach (var word in new[] { "valorant", "gaming", "game", "văn phòng", "do hoa", "đồ họa", "livestream", "stream", "rtx", "rx", "ryzen", "intel", "logitech", "razer", "akko", "asus", "msi", "gigabyte", "corsair", "g304", "g502" })
            if (lower.Contains(word)) tokens.Add(word);
        foreach (Match match in ProductTokenRegex().Matches(NormalizeText(text)))
        {
            var value = match.Value;
            if (!IgnoredSearchTokens.Contains(value)) tokens.Add(value);
        }
        if (!string.IsNullOrWhiteSpace(analysis.TargetGame)) tokens.Add(analysis.TargetGame);
        return tokens.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool DetectSpecificProductName(string normalized, string? componentType)
    {
        if (Regex.IsMatch(normalized, @"\b(?:rtx|gtx|rx|i[3579]|ryzen)\s*\d{3,5}[a-z]*\b", RegexOptions.IgnoreCase)) return true;
        if (Regex.IsMatch(normalized, @"\b[a-z]{2,}\s*[a-z]*\s*\d{3,5}[a-z]*\b", RegexOptions.IgnoreCase)) return true;
        return !string.IsNullOrWhiteSpace(componentType) && BrandWords.Any(normalized.Contains);
    }

    private static bool ContainsAny(string source, params string[] values) => values.Any(source.Contains);

    private static bool ContainsIgnoreCase(string? source, string value) => !string.IsNullOrWhiteSpace(source) && source.Contains(value, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeText(string value)
    {
        var normalized = value.Replace('đ', 'd').Replace('Đ', 'D').Normalize(System.Text.NormalizationForm.FormD);
        return new string(normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()).Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant();
    }

    private static string TrimText(string value, int max) => string.IsNullOrWhiteSpace(value) ? "Đang cập nhật" : value.Length <= max ? value : value[..max] + "...";

    [GeneratedRegex(@"(?:dưới|duoi|khoảng|tam|tầm|<=|<)?\s*(\d+(?:[\.,]\d+)?)\s*(?:triệu|tr|m|million)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BudgetRegex();

    [GeneratedRegex(@"\b(?:rtx\s*\d{4}|rx\s*\d{4}|gtx\s*\d{4}|i[3579](?:-\d{4,5}[a-z]*)?|ryzen\s*[3579](?:\s*\d{4}[a-z]*)?)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HardwareRegex();


    [GeneratedRegex(@"(\d{2,3})\s*fps", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FpsRegex();

    [GeneratedRegex(@"\b[a-z0-9]{3,}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ProductTokenRegex();

    private static readonly string[] PcCategoryNames = ["PC Gaming", "Workstation", "AMD Gaming", "PC Mini", "PC Văn Phòng"];
    private static readonly string[] PcIntentWords = ["cau hinh", "pc", "may tinh", "choi game", "gaming", "fps", "setting", "build", "stream", "do hoa", "valorant", "gta", "black myth"];
    private static readonly Dictionary<string, string> GameWords = new(StringComparer.OrdinalIgnoreCase) { ["valorant"] = "Valorant", ["gta"] = "GTA", ["black myth"] = "Black Myth" };
    private static readonly string[] BrandWords = ["logitech", "razer", "akko", "asus", "msi", "gigabyte", "corsair", "cooler master", "kingston", "samsung"];
    private static readonly HashSet<string> IgnoredSearchTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "toi", "minh", "ban", "shop", "can", "tim", "mua", "gia", "bao", "nhieu", "con", "hang", "cho", "voi", "mot", "cai", "chiec", "san", "pham", "chuot", "mouse", "ban", "phim", "keyboard", "tai", "nghe", "headset", "man", "hinh", "monitor", "linh", "kien", "may", "tinh", "duoi", "tam", "khoang", "trieu"
    };
    private static readonly Dictionary<string, string[]> ComponentIntentWords = new(StringComparer.OrdinalIgnoreCase)
    {
        [ComponentTypes.Headphone] = ["tai nghe", "headphone", "headset"],
        [ComponentTypes.Keyboard] = ["ban phim", "keyboard"],
        [ComponentTypes.Mouse] = ["chuot", "mouse"],
        [ComponentTypes.Monitor] = ["man hinh", "monitor"],
        [ComponentTypes.RAM] = [" ram", "bo nho"],
        [ComponentTypes.Storage] = ["ssd", "hdd", "o cung"],
        [ComponentTypes.VGA] = ["vga", "gpu", "card man hinh"],
        [ComponentTypes.CPU] = ["cpu", "bo vi xu ly"]
    };

    private sealed record AiSearchAnalysis(string Intent, string CategoryScope, decimal? Budget, string? TargetGame, int? TargetFps, string? ComponentType, bool HasSpecificProductName);
}
