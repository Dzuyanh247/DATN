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
    string Warranty = "",
    string ProductTypeLabel = "Linh kiện");

public interface IProductSearchForAiService
{
    Task<IReadOnlyList<AiProductContext>> SearchAsync(string message, CancellationToken cancellationToken = default);
    Task<AiProductContext?> GetByIdAsync(int productId, CancellationToken cancellationToken = default);
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

    public async Task<AiProductContext?> GetByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.AsNoTracking().Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == productId && x.IsActive, cancellationToken);
        if (product == null) return null;
        var scope = IsPcProduct(product) ? "PC" : ComponentTypes.Normalize(product.ComponentType);
        return ToContext(product, scope);
    }

    public async Task<IReadOnlyList<AiProductContext>> SearchAsync(string message, CancellationToken cancellationToken = default)
    {
        message = (message ?? string.Empty).Trim();
        var max = Math.Clamp(_options.MaxProductsContext, 1, 10);
        var analysis = Analyze(message);
        var tokens = ExtractTokens(message, analysis);

        var inStockActiveQuery = _db.Products.AsNoTracking().Include(x => x.Category)
            .Where(x => x.IsActive && (x.StockQuantity > 0 || x.IsInStock));
        var activeCount = await inStockActiveQuery.CountAsync(cancellationToken);

        var baseQuery = analysis.CategoryScope == "PC"
            ? ApplyPcScope(inStockActiveQuery)
            : ApplyComponentScope(inStockActiveQuery, analysis.ComponentType);
        var scopedCount = await baseQuery.CountAsync(cancellationToken);

        if (analysis.BudgetMin.HasValue)
            baseQuery = baseQuery.Where(x => (x.DiscountPrice ?? x.SalePrice ?? x.Price) >= analysis.BudgetMin.Value);
        if (analysis.BudgetMax.HasValue)
            baseQuery = baseQuery.Where(x => (x.DiscountPrice ?? x.SalePrice ?? x.Price) <= analysis.BudgetMax.Value);
        var budgetCount = await baseQuery.CountAsync(cancellationToken);

        List<AiProductContext> products;
        if (tokens.Count > 0)
        {
            var sample = await baseQuery.OrderBy(x => x.DiscountPrice ?? x.SalePrice ?? x.Price).Take(120).ToListAsync(cancellationToken);
            var minScore = analysis.HasSpecificProductName && tokens.Count > 1 ? 2 : 1;
            products = sample
                .Where(x => ProductAllowedForIntent(x, analysis))
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
            var sample = await baseQuery.OrderBy(x => x.DiscountPrice ?? x.SalePrice ?? x.Price).Take(120).ToListAsync(cancellationToken);
            products = sample
                .Where(x => ProductAllowedForIntent(x, analysis))
                .Take(max)
                .Select(x => ToContext(x, analysis.CategoryScope))
                .ToList();
        }

        var fallbackReason = "none";
        if (products.Count == 0 && analysis.CategoryScope == "PC")
        {
            fallbackReason = "no_pc_in_exact_budget_or_keyword";
            products = await FindNearestPcAsync(inStockActiveQuery, analysis, max, cancellationToken);
        }

        if (products.Count == 0 && analysis.CategoryScope == "PC")
        {
            fallbackReason = "no_pc_after_nearest_budget_try_build_components";
            products = await BuildComponentFallbackAsync(inStockActiveQuery, analysis, max, cancellationToken);
        }

        products = products.Where(p => ContextAllowedForIntent(p, analysis)).ToList();
        LogSearch(message, analysis, products, activeCount, scopedCount, budgetCount, fallbackReason);
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
            string.IsNullOrWhiteSpace(x.Name) ? "Sản phẩm không xác định" : x.Name,
            x.Price,
            TrimText(string.IsNullOrWhiteSpace(x.Specifications) ? x.ShortDescription ?? string.Empty : x.Specifications!, 450),
            x.StockQuantity > 0 ? $"Còn hàng ({x.StockQuantity})" : "Tạm hết hàng",
            $"/Products/Detail/{x.Id}",
            string.IsNullOrWhiteSpace(x.Category) ? "Chưa phân loại" : x.Category,
            x.StockQuantity,
            scope, TrimText(x.Description ?? string.Empty, 300), !string.IsNullOrWhiteSpace(x.WarrantyDuration) ? x.WarrantyDuration : $"{x.WarrantyMonths} tháng", LabelForScope(scope))).ToList();
    }


    private static IQueryable<Product> ApplyPcScope(IQueryable<Product> query) => query.Where(x =>
        x.ProductType == ProductKinds.PC ||
        x.ComponentType == "PC" ||
        (x.Category != null && (PcCategoryNames.Contains(x.Category.Name) || x.Category.Name.Contains("PC") || x.Category.Name.Contains("Cấu hình") || x.Category.Name.Contains("Máy bộ") || x.Category.Name.Contains("Máy tính"))));

    private static IQueryable<Product> ApplyComponentScope(IQueryable<Product> query, string? componentType)
    {
        query = query.Where(x => x.ProductType == ProductKinds.Component || x.ProductType == ProductKinds.PC || (x.Category != null && (x.Category.Name == "Linh kiện" || x.Category.Name == "Màn hình" || x.Category.Name.Contains("Phụ kiện") || x.Category.Name.Contains("Bàn phím") || x.Category.Name.Contains("Chuột") || x.Category.Name.Contains("Tai nghe"))));
        if (string.IsNullOrWhiteSpace(componentType)) return query;
        var normalized = ComponentTypes.Normalize(componentType);
        return normalized switch
        {
            ComponentTypes.CPU => query.Where(x => x.ComponentType == ComponentTypes.CPU || (x.Category != null && x.Category.Name.Contains("CPU")) || x.Name.Contains("CPU") || x.Name.Contains("Intel") || x.Name.Contains("Ryzen")),
            ComponentTypes.Mainboard => query.Where(x => x.ComponentType == ComponentTypes.Mainboard || (x.Category != null && x.Category.Name.Contains("Main")) || x.Name.Contains("Main") || x.Name.Contains("Bo mạch")),
            ComponentTypes.RAM => query.Where(x => x.ComponentType == ComponentTypes.RAM || (x.Category != null && x.Category.Name.Contains("RAM")) || x.Name.Contains("RAM")),
            ComponentTypes.VGA => query.Where(x => x.ComponentType == ComponentTypes.VGA || (x.Category != null && (x.Category.Name.Contains("VGA") || x.Category.Name.Contains("Card"))) || x.Name.Contains("RTX") || x.Name.Contains("GTX") || x.Name.Contains("RX ") || x.Name.Contains("VGA")),
            ComponentTypes.Storage => query.Where(x => x.ComponentType == ComponentTypes.Storage || (x.Category != null && (x.Category.Name.Contains("SSD") || x.Category.Name.Contains("HDD") || x.Category.Name.Contains("Ổ cứng"))) || x.Name.Contains("SSD") || x.Name.Contains("HDD")),
            ComponentTypes.PSU => query.Where(x => x.ComponentType == ComponentTypes.PSU || (x.Category != null && (x.Category.Name.Contains("Nguồn") || x.Category.Name.Contains("PSU"))) || x.Name.Contains("Nguồn") || x.Name.Contains("PSU")),
            ComponentTypes.Case => query.Where(x => x.ComponentType == ComponentTypes.Case || (x.Category != null && (x.Category.Name.Contains("Case") || x.Category.Name.Contains("Vỏ"))) || x.Name.Contains("Case") || x.Name.Contains("Vỏ case")),
            ComponentTypes.Cooler => query.Where(x => x.ComponentType == ComponentTypes.Cooler || (x.Category != null && (x.Category.Name.Contains("Tản") || x.Category.Name.Contains("Cooler"))) || x.Name.Contains("Tản") || x.Name.Contains("Cooler")),
            ComponentTypes.Keyboard => query.Where(x => x.ComponentType == ComponentTypes.Keyboard || (x.Category != null && x.Category.Name.Contains("Bàn phím")) || x.Name.Contains("Bàn phím") || x.Name.Contains("Keyboard") || x.Name.Contains("Keycap")),
            ComponentTypes.Mouse => query.Where(x => x.ComponentType == ComponentTypes.Mouse || (x.Category != null && x.Category.Name.Contains("Chuột")) || x.Name.Contains("Chuột") || x.Name.Contains("Mouse")),
            ComponentTypes.Headphone => query.Where(x => x.ComponentType == ComponentTypes.Headphone || (x.Category != null && x.Category.Name.Contains("Tai nghe")) || x.Name.Contains("Tai nghe") || x.Name.Contains("Headset") || x.Name.Contains("Headphone")),
            ComponentTypes.Monitor => query.Where(x => x.ComponentType == ComponentTypes.Monitor || (x.Category != null && (x.Category.Name.Contains("Màn hình") || x.Category.Name.Contains("Monitor"))) || x.Name.Contains("Màn hình") || x.Name.Contains("Monitor")),
            _ => query.Where(x => x.ComponentType == normalized)
        };
    }

    private static AiProductContext ToContext(Product x, string scope) => new(
        x.Id, string.IsNullOrWhiteSpace(x.Name) ? "Sản phẩm không xác định" : x.Name, x.DiscountPrice ?? x.SalePrice ?? x.Price,
        TrimText(string.IsNullOrWhiteSpace(x.Specifications) ? x.ShortDescription ?? string.Empty : x.Specifications!, 450),
        x.StockQuantity > 0 ? $"Còn hàng ({x.StockQuantity})" : "Tạm hết hàng",
        $"/Products/Detail/{x.Id}", x.Category?.Name ?? "Chưa phân loại", x.StockQuantity, scope,
        TrimText(x.Description ?? string.Empty, 300), !string.IsNullOrWhiteSpace(x.WarrantyDuration) ? x.WarrantyDuration : $"{x.WarrantyMonths} tháng", LabelForScope(scope, x));

    private void LogSearch(string message, AiSearchAnalysis analysis, IReadOnlyList<AiProductContext> products, int activeCount, int scopedCount, int budgetCount, string fallbackReason)
    {
        _logger.LogInformation("[AI_PRODUCT_SEARCH] message={Message}; intent={Intent}; budgetMin={BudgetMin}; budgetMax={BudgetMax}; keywordGame={TargetGame}; componentType={ComponentType}; requestedProductType={RequestedProductType}; excludedTypes={ExcludedTypes}; categoryScope={Scope}; activeStockProducts={ActiveCount}; scopedProducts={ScopedCount}; budgetProducts={BudgetCount}; finalProducts={Products}; fallbackReason={FallbackReason}; topProducts={TopProducts}",
            TrimText(message, 180), analysis.Intent, analysis.BudgetMin, analysis.BudgetMax, analysis.TargetGame ?? "none", analysis.ComponentType ?? "none", analysis.CategoryScope == "PC" ? "PC/build" : analysis.ComponentType ?? "product", analysis.CategoryScope == "PC" ? string.Join(",", AccessoryTypes) : "none", analysis.CategoryScope, activeCount, scopedCount, budgetCount, products.Count, fallbackReason, string.Join(" | ", products.Take(5).Select(p => $"{p.Id}:{p.Name}:{p.Price:N0}:{p.Category}:{p.CategoryScope}:{p.ProductTypeLabel}")));
    }

    private static int ScoreProduct(Product product, IReadOnlyList<string> tokens, AiSearchAnalysis analysis)
    {
        var haystack = $"{product.Name} {product.ShortDescription} {product.Description} {product.Specifications} {product.Category?.Name}";
        var score = tokens.Count(token => ContainsIgnoreCase(haystack, token));
        if (analysis.CategoryScope == "PC" && IsPcProduct(product)) score += 5;
        if (!string.IsNullOrWhiteSpace(analysis.TargetGame) && ContainsIgnoreCase(haystack, analysis.TargetGame)) score += 3;
        return score;
    }

    private static AiSearchAnalysis Analyze(string text)
    {
        var normalized = NormalizeText(text);
        var componentType = DetectComponentType(normalized);
        var game = GameWords.FirstOrDefault(g => normalized.Contains(g.Key)).Value;
        var accessory = DetectAccessoryType(normalized);
        if (accessory != null) componentType = accessory;
        var isCompare = ContainsAny(normalized, "so sanh", "compare", "khac nhau");
        var isPcAdvice = (PcIntentWords.Any(normalized.Contains) || !string.IsNullOrWhiteSpace(game)) && accessory == null;
        var isPolicy = ContainsAny(normalized, "tra gop", "freeship", "doi tra", "bao hanh tan noi", "hoa toc", "chinh sach");
        var isOrder = ContainsAny(normalized, "don hang", "ma don", "dh0");
        var isWarranty = ContainsAny(normalized, "bao hanh", "warranty");
        var isPayment = ContainsAny(normalized, "thanh toan", "chuyen khoan", "cod");
        var isHuman = ContainsAny(normalized, "nhan vien", "tu van vien", "nguoi that");
        var intent = isHuman ? "HUMAN_SUPPORT" : isOrder ? "ORDER_QA" : isPayment ? "PAYMENT_QA" : isWarranty ? "WARRANTY_QA" : isPolicy ? "POLICY_QA" : isPcAdvice ? "PC_ADVICE" : isCompare ? "PRODUCT_COMPARE" : "PRODUCT_QA";
        var hasSpecificProductName = DetectSpecificProductName(normalized, componentType);
        var budget = ParseBudget(text);
        return new AiSearchAnalysis(intent, isPcAdvice ? "PC" : "COMPONENT", budget.Min, budget.Max, game, ParseFps(normalized), componentType, hasSpecificProductName);
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

    private static (decimal? Min, decimal? Max) ParseBudget(string text)
    {
        var normalized = NormalizeText(text);
        var range = BudgetRangeRegex().Match(normalized);
        if (range.Success && TryMoney(range.Groups[1].Value, out var min) && TryMoney(range.Groups[2].Value, out var max)) return (min, max);
        var match = BudgetRegex().Match(normalized);
        if (!match.Success || !TryMoney(match.Groups[2].Value, out var amount)) return (null, null);
        var prefix = match.Groups[1].Value;
        if (prefix.Contains("duoi") || prefix.Contains("<")) return (null, amount);
        if (prefix.Contains("tren") || prefix.Contains(">")) return (amount, null);
        if (prefix.Contains("tam") || prefix.Contains("khoang")) return (amount * 0.85m, amount * 1.15m);
        return (null, amount);
    }

    private static bool TryMoney(string value, out decimal result)
    {
        result = 0;
        if (!decimal.TryParse(value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)) return false;
        result = amount < 1000 ? amount * 1_000_000 : amount;
        return true;
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

    [GeneratedRegex(@"(duoi|khoang|tam|tren|>=|>|<=|<)?\s*(\d+(?:[\.,]\d+)?)\s*(?:trieu|tr|m|million)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BudgetRegex();

    [GeneratedRegex(@"(\d+(?:[\.,]\d+)?)\s*(?:-|den|toi)\s*(\d+(?:[\.,]\d+)?)\s*(?:trieu|tr|m|million)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BudgetRangeRegex();

    [GeneratedRegex(@"\b(?:rtx\s*\d{4}|rx\s*\d{4}|gtx\s*\d{4}|i[3579](?:-\d{4,5}[a-z]*)?|ryzen\s*[3579](?:\s*\d{4}[a-z]*)?)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HardwareRegex();


    [GeneratedRegex(@"(\d{2,3})\s*fps", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FpsRegex();

    [GeneratedRegex(@"\b[a-z0-9]{3,}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ProductTokenRegex();

    private static readonly string[] PcCategoryNames = ["PC Gaming", "Workstation", "AMD Gaming", "PC Mini", "PC Văn Phòng", "PC", "Máy bộ", "Cấu hình", "Máy tính"];
    private static readonly string[] PcIntentWords = ["cau hinh", "pc", "may tinh", "may bo", "may choi game", "choi game", "gaming", "fps", "setting", "build", "stream", "do hoa", "valorant", "gta", "gta5", "gta v", "black myth", "shop co san"];
    private static readonly Dictionary<string, string> GameWords = new(StringComparer.OrdinalIgnoreCase) { ["valorant"] = "Valorant", ["gta5"] = "GTA V", ["gta v"] = "GTA V", ["gta"] = "GTA V", ["black myth"] = "Black Myth" };
    private static readonly string[] BrandWords = ["logitech", "razer", "akko", "asus", "msi", "gigabyte", "corsair", "cooler master", "kingston", "samsung"];
    private static readonly HashSet<string> IgnoredSearchTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "toi", "minh", "ban", "shop", "can", "tim", "mua", "gia", "bao", "nhieu", "con", "hang", "cho", "voi", "mot", "cai", "chiec", "san", "pham", "chuot", "mouse", "ban", "phim", "keyboard", "tai", "nghe", "headset", "man", "hinh", "monitor", "linh", "kien", "may", "tinh", "duoi", "tam", "khoang", "trieu", "cau", "hinh", "duoc", "khong", "thi", "sao", "co", "san", "di"
    };
    private static readonly Dictionary<string, string[]> ComponentIntentWords = new(StringComparer.OrdinalIgnoreCase)
    {
        [ComponentTypes.Headphone] = ["tai nghe", "headphone", "headset"],
        [ComponentTypes.Keyboard] = ["ban phim", "keyboard", "keycap", "ban phim co"],
        [ComponentTypes.Mouse] = ["chuot", "mouse"],
        [ComponentTypes.Monitor] = ["man hinh", "monitor"],
        [ComponentTypes.RAM] = [" ram", "bo nho"],
        [ComponentTypes.Storage] = ["ssd", "hdd", "o cung"],
        [ComponentTypes.VGA] = ["vga", "gpu", "card", "card man hinh"],
        [ComponentTypes.CPU] = ["cpu", "bo vi xu ly"],
        [ComponentTypes.Mainboard] = ["mainboard", "main", "bo mach chu"],
        [ComponentTypes.PSU] = ["psu", "nguon"],
        [ComponentTypes.Case] = ["case", "vo case"],
        [ComponentTypes.Cooler] = ["tan nhiet", "cooler"]
    };


    private static readonly HashSet<string> AccessoryTypes = new(StringComparer.OrdinalIgnoreCase) { ComponentTypes.Mouse, ComponentTypes.Keyboard, ComponentTypes.Headphone, ComponentTypes.Monitor, ComponentTypes.MonitorArm };
    private static readonly string[] AccessoryWords = ["chuot", "mouse", "ban phim", "keyboard", "tai nghe", "headset", "headphone", "man hinh", "monitor", "phu kien"];
    private static readonly Dictionary<string, string[]> AccessoryIntentWords = new(StringComparer.OrdinalIgnoreCase)
    {
        [ComponentTypes.Headphone] = ["tai nghe", "headphone", "headset"],
        [ComponentTypes.Keyboard] = ["ban phim", "keyboard", "keycap", "ban phim co"],
        [ComponentTypes.Mouse] = ["chuot", "mouse"],
        [ComponentTypes.Monitor] = ["man hinh", "monitor"]
    };

    private static bool IsPcProduct(Product product) => product.ProductType == ProductKinds.PC || product.ComponentType == "PC" || PcCategoryNames.Contains(product.Category?.Name ?? string.Empty) || ContainsAny(product.Category?.Name ?? string.Empty, "PC", "Cấu hình", "Máy bộ", "Máy tính");


    private static string? DetectAccessoryType(string normalized)
    {
        foreach (var item in AccessoryIntentWords)
            if (item.Value.Any(normalized.Contains)) return item.Key;
        return null;
    }

    private static bool ProductAllowedForIntent(Product product, AiSearchAnalysis analysis)
    {
        if (analysis.CategoryScope == "PC") return IsPcProduct(product) && !IsAccessoryProduct(product);
        if (!string.IsNullOrWhiteSpace(analysis.ComponentType)) return ComponentTypes.Normalize(product.ComponentType) == ComponentTypes.Normalize(analysis.ComponentType) || MatchesComponentByText(product, analysis.ComponentType);
        return !IsAccessoryProduct(product);
    }

    private static bool ContextAllowedForIntent(AiProductContext product, AiSearchAnalysis analysis)
    {
        if (analysis.CategoryScope == "PC") return product.CategoryScope == "PC" || product.CategoryScope.StartsWith("BUILD_", StringComparison.OrdinalIgnoreCase);
        return string.IsNullOrWhiteSpace(analysis.ComponentType) || string.Equals(product.CategoryScope, analysis.ComponentType, StringComparison.OrdinalIgnoreCase) || string.Equals(product.ProductTypeLabel, LabelForComponent(analysis.ComponentType), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAccessoryProduct(Product product)
    {
        var type = ComponentTypes.Normalize(product.ComponentType);
        if (AccessoryTypes.Contains(type)) return true;
        var text = NormalizeText($"{product.Name} {product.Category?.Name} {product.ProductType}");
        return AccessoryWords.Any(text.Contains);
    }

    private static bool MatchesComponentByText(Product product, string componentType)
    {
        var normalized = ComponentTypes.Normalize(componentType);
        var text = NormalizeText($"{product.Name} {product.Category?.Name} {product.ComponentType}");
        return normalized switch
        {
            ComponentTypes.CPU => ContainsAny(text, "cpu", "ryzen", "intel", "core i"),
            ComponentTypes.Mainboard => ContainsAny(text, "mainboard", "main", "bo mach chu"),
            ComponentTypes.RAM => ContainsAny(text, "ram", "bo nho"),
            ComponentTypes.VGA => ContainsAny(text, "vga", "gpu", "card man hinh", "rtx", "gtx", "rx "),
            ComponentTypes.Storage => ContainsAny(text, "ssd", "hdd", "o cung"),
            ComponentTypes.PSU => ContainsAny(text, "psu", "nguon"),
            ComponentTypes.Case => ContainsAny(text, "case", "vo case"),
            ComponentTypes.Cooler => ContainsAny(text, "tan nhiet", "cooler"),
            ComponentTypes.Monitor => ContainsAny(text, "monitor", "man hinh"),
            ComponentTypes.Keyboard => ContainsAny(text, "keyboard", "keycap", "ban phim"),
            ComponentTypes.Mouse => ContainsAny(text, "mouse", "chuot"),
            ComponentTypes.Headphone => ContainsAny(text, "headphone", "headset", "tai nghe"),
            _ => false
        };
    }

    private static string LabelForScope(string scope, Product? product = null)
    {
        if (string.Equals(scope, "PC", StringComparison.OrdinalIgnoreCase)) return "PC đề xuất";
        if (scope.StartsWith("BUILD_", StringComparison.OrdinalIgnoreCase)) return LabelForComponent(scope[6..]);
        return LabelForComponent(product?.ComponentType ?? scope);
    }

    private static string LabelForComponent(string? componentType) => ComponentTypes.Normalize(componentType) switch
    {
        ComponentTypes.CPU => "CPU",
        ComponentTypes.VGA => "VGA",
        ComponentTypes.Mainboard => "Mainboard",
        ComponentTypes.RAM => "RAM",
        ComponentTypes.Storage => "Ổ cứng",
        ComponentTypes.PSU => "Nguồn",
        ComponentTypes.Case => "Vỏ case",
        ComponentTypes.Cooler => "Tản nhiệt",
        ComponentTypes.Mouse => "Chuột",
        ComponentTypes.Keyboard => "Bàn phím",
        ComponentTypes.Headphone => "Tai nghe",
        ComponentTypes.Monitor => "Màn hình",
        _ => "Linh kiện"
    };

    private async Task<List<AiProductContext>> FindNearestPcAsync(IQueryable<Product> inStockActiveQuery, AiSearchAnalysis analysis, int max, CancellationToken ct)
    {
        var target = analysis.BudgetMax ?? analysis.BudgetMin;
        var candidates = (await ApplyPcScope(inStockActiveQuery).Take(200).ToListAsync(ct))
            .Where(x => !IsAccessoryProduct(x))
            .ToList();
        var ordered = target.HasValue
            ? candidates.OrderBy(x => Math.Abs((x.DiscountPrice ?? x.SalePrice ?? x.Price) - target.Value))
            : candidates.OrderBy(x => x.DiscountPrice ?? x.SalePrice ?? x.Price);
        return ordered.Take(max).Select(x => ToContext(x, "PC")).ToList();
    }

    private async Task<List<AiProductContext>> BuildComponentFallbackAsync(IQueryable<Product> inStockActiveQuery, AiSearchAnalysis analysis, int max, CancellationToken ct)
    {
        var target = analysis.BudgetMax ?? analysis.BudgetMin ?? 20_000_000m;
        var budgetShare = new Dictionary<string, decimal>
        {
            [ComponentTypes.CPU] = 0.18m, [ComponentTypes.Mainboard] = 0.12m, [ComponentTypes.RAM] = 0.10m, [ComponentTypes.VGA] = 0.35m,
            [ComponentTypes.Storage] = 0.10m, [ComponentTypes.PSU] = 0.08m, [ComponentTypes.Case] = 0.05m, [ComponentTypes.Cooler] = 0.02m
        };
        var selected = new List<AiProductContext>();
        foreach (var type in budgetShare.Keys)
        {
            var componentQuery = ApplyComponentScope(inStockActiveQuery, type);
            var candidates = (await componentQuery.Take(100).ToListAsync(ct))
                .Where(x => !IsAccessoryProduct(x))
                .ToList();
            var item = candidates
                .OrderByDescending(x => x.StockQuantity > 0)
                .ThenBy(x => Math.Abs((x.DiscountPrice ?? x.SalePrice ?? x.Price) - target * budgetShare[type]))
                .FirstOrDefault();
            if (item != null) selected.Add(ToContext(item, $"BUILD_{type}"));
        }
        var missing = budgetShare.Keys.Where(type => selected.All(item => !string.Equals(item.CategoryScope, $"BUILD_{type}", StringComparison.OrdinalIgnoreCase))).ToList();
        if (missing.Count > 0)
        {
            _logger.LogWarning("[AI_PRODUCT_SEARCH] build fallback missing components: {MissingComponents}", string.Join(",", missing));
            return [];
        }
        return selected.Take(max).ToList();
    }

    private sealed record AiSearchAnalysis(string Intent, string CategoryScope, decimal? BudgetMin, decimal? BudgetMax, string? TargetGame, int? TargetFps, string? ComponentType, bool HasSpecificProductName);
}
