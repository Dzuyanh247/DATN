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
    string ProductTypeLabel = "Linh kiện",
    string ImageUrl = "",
    bool CanAddToBuild = false);

public record AiSalesSearchPlan(string Intent, string CategoryScope, string? ComponentType, decimal? BudgetTarget, decimal? BudgetMin, decimal? BudgetMax, string? Purpose, string? Game, IReadOnlyList<string> SearchSignals, bool AllowBuildFallback, string PriceMode = "normal");

public interface IProductSearchForAiService
{
    Task<IReadOnlyList<AiProductContext>> SearchByPlanAsync(AiSalesSearchPlan plan, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiProductContext>> SearchAsync(string message, CancellationToken cancellationToken = default);
    Task<AiProductContext?> GetByIdAsync(int productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiProductContext>> QueryByIntentAsync(string intent, string? productType, string priceMode, decimal? budgetTarget, CancellationToken cancellationToken = default);
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
    public async Task<IReadOnlyList<AiProductContext>> SearchByPlanAsync(AiSalesSearchPlan plan, CancellationToken cancellationToken = default)
    {
        var max = Math.Clamp(_options.MaxProductsContext, 1, 3);
        var inStockActiveQuery = _db.Products.AsNoTracking().Include(x => x.Category)
            .Where(x => x.IsActive && (x.StockQuantity > 0 || x.IsInStock));
        var scope = string.Equals(plan.CategoryScope, "PC", StringComparison.OrdinalIgnoreCase) ? "PC" : "COMPONENT";
        var baseQuery = scope == "PC" ? ApplyPcScope(inStockActiveQuery) : ApplyComponentScope(inStockActiveQuery, plan.ComponentType);
        var activeCount = await inStockActiveQuery.CountAsync(cancellationToken);
        var scopedCount = await baseQuery.CountAsync(cancellationToken);
        var normalizedSignals = plan.SearchSignals.Select(NormalizeText).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        async Task<List<Product>> LoadBudgetWindow(decimal min, decimal maxPrice) => await baseQuery
            .Where(x => (x.DiscountPrice ?? x.SalePrice ?? x.Price) >= min && (x.DiscountPrice ?? x.SalePrice ?? x.Price) <= maxPrice)
            .Take(240).ToListAsync(cancellationToken);

        List<Product> candidates;
        var budgetStep = "none";
        if (plan.BudgetTarget.HasValue && plan.PriceMode == "normal")
        {
            var target = plan.BudgetTarget.Value;
            candidates = await LoadBudgetWindow(target * 0.90m, target * 1.10m);
            budgetStep = "target_90_110";
            if (candidates.Count == 0)
            {
                candidates = await LoadBudgetWindow(target * 0.85m, target * 1.15m);
                budgetStep = "target_85_115";
            }
            if (candidates.Count == 0)
            {
                candidates = await baseQuery.Take(240).ToListAsync(cancellationToken);
                budgetStep = "nearest_price";
            }
        }
        else
        {
            var q = baseQuery;
            if (plan.BudgetMin.HasValue) q = q.Where(x => (x.DiscountPrice ?? x.SalePrice ?? x.Price) >= plan.BudgetMin.Value);
            if (plan.BudgetMax.HasValue) q = q.Where(x => (x.DiscountPrice ?? x.SalePrice ?? x.Price) <= plan.BudgetMax.Value);
            candidates = await q.Take(240).ToListAsync(cancellationToken);
        }

        var ordered = candidates
            .Where(x => scope == "PC" ? IsPcProduct(x) && !IsAccessoryProduct(x) : ProductAllowedForIntent(x, new AiSearchAnalysis(plan.Intent, "COMPONENT", plan.BudgetMin, plan.BudgetMax, plan.Game, null, plan.ComponentType, false)))
            .Select(x => new { Product = x, Score = ScoreSalesPlan(x, plan, normalizedSignals) })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => plan.BudgetTarget.HasValue ? Math.Abs((x.Product.DiscountPrice ?? x.Product.SalePrice ?? x.Product.Price) - plan.BudgetTarget.Value) : (x.Product.DiscountPrice ?? x.Product.SalePrice ?? x.Product.Price))
            .Take(max)
            .Select(x => ToContext(x.Product, scope == "PC" ? "PC" : ComponentTypes.Normalize(plan.ComponentType)))
            .ToList();

        if (ordered.Count == 0 && scope == "PC" && plan.AllowBuildFallback)
            ordered = await BuildComponentFallbackAsync(inStockActiveQuery, new AiSearchAnalysis(plan.Intent, "PC", plan.BudgetMin, plan.BudgetMax ?? plan.BudgetTarget, plan.Game, null, null, false), max, cancellationToken);

        _logger.LogInformation("[AI_PLANNER_SEARCH] intent={Intent}; scope={Scope}; component={Component}; budgetTarget={BudgetTarget}; budgetMin={BudgetMin}; budgetMax={BudgetMax}; purpose={Purpose}; game={Game}; signals={Signals}; budgetStep={BudgetStep}; active={Active}; scoped={Scoped}; products={Products}; topProducts={TopProducts}",
            plan.Intent, plan.CategoryScope, plan.ComponentType ?? "none", plan.BudgetTarget, plan.BudgetMin, plan.BudgetMax, plan.Purpose ?? "none", plan.Game ?? "none", string.Join(",", normalizedSignals), budgetStep, activeCount, scopedCount, ordered.Count, string.Join(" | ", ordered.Select(p => $"{p.Id}:{p.Name}:{p.Price:N0}:{p.StockStatus}")));
        return ordered;
    }

    private static decimal ScoreSalesPlan(Product product, AiSalesSearchPlan plan, IReadOnlyList<string> signals)
    {
        var haystack = NormalizeText($"{product.Name} {product.ShortDescription} {product.Description} {product.Specifications} {product.Category?.Name} {product.ComponentType}");
        decimal score = 0;
        if (signals.Any(s => haystack.Contains(s))) score += signals.Count(s => haystack.Contains(s)) * 12;
        if (product.StockQuantity > 0 || product.IsInStock) score += 25;
        if (product.DiscountPrice.HasValue && product.DiscountPrice.Value < product.Price) score += 8;
        if (plan.BudgetTarget.HasValue) score += Math.Max(0, 30 - Math.Abs((product.DiscountPrice ?? product.SalePrice ?? product.Price) - plan.BudgetTarget.Value) / Math.Max(1, plan.BudgetTarget.Value) * 30);
        if (string.Equals(plan.Purpose, "Gaming", StringComparison.OrdinalIgnoreCase))
        {
            if (ContainsAny(haystack, "rtx 4060", "rtx 5060", "rtx 4070", "rtx 5070", "rx 7600")) score += 18;
            if (ContainsAny(haystack, "16gb", "32gb")) score += 10;
            if (ContainsAny(haystack, "ssd", "512", "1tb")) score += 8;
        }
        if (!string.IsNullOrWhiteSpace(plan.Game) && haystack.Contains(NormalizeText(plan.Game))) score += 8;
        return score;
    }


    public async Task<AiProductContext?> GetByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.AsNoTracking().Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == productId && x.IsActive, cancellationToken);
        if (product == null) return null;
        var scope = IsPcProduct(product) ? "PC" : ComponentTypes.Normalize(product.ComponentType);
        return ToContext(product, scope);
    }


    public async Task<IReadOnlyList<AiProductContext>> QueryByIntentAsync(string intent, string? productType, string priceMode, decimal? budgetTarget, CancellationToken cancellationToken = default)
    {
        var max = Math.Clamp(_options.MaxProductsContext, 1, 3);
        var query = _db.Products.AsNoTracking().Include(x => x.Category)
            .Where(x => x.IsActive && (x.DiscountPrice ?? x.SalePrice ?? x.Price) > 0 && (x.StockQuantity > 0 || x.IsInStock));
        var isPcIntent = string.Equals(intent, "PC_BUILD_ADVICE", StringComparison.OrdinalIgnoreCase);
        var isExtremeAll = string.Equals(intent, "PRODUCT_EXTREME_QUERY", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(productType);
        var scope = isPcIntent ? "PC" : isExtremeAll ? "ALL" : ComponentTypes.Normalize(productType);
        query = scope == "PC" ? ApplyPcScope(query) : scope == "ALL" ? query : ApplyComponentScope(query, scope);
        if (string.Equals(priceMode, "highest", StringComparison.OrdinalIgnoreCase))
        {
            var rows = await query.OrderByDescending(x => x.DiscountPrice ?? x.SalePrice ?? x.Price).Take(Math.Min(3, max)).ToListAsync(cancellationToken);
            return rows.Select(x => ToContext(x, scope == "PC" ? "PC" : scope == "ALL" ? (IsPcProduct(x) ? "PC" : ComponentTypes.Normalize(x.ComponentType)) : ComponentTypes.Normalize(productType))).ToList();
        }
        if (string.Equals(priceMode, "lowest", StringComparison.OrdinalIgnoreCase))
        {
            var rows = await query.OrderBy(x => x.DiscountPrice ?? x.SalePrice ?? x.Price).Take(Math.Min(3, max)).ToListAsync(cancellationToken);
            return rows.Select(x => ToContext(x, scope == "PC" ? "PC" : scope == "ALL" ? (IsPcProduct(x) ? "PC" : ComponentTypes.Normalize(x.ComponentType)) : ComponentTypes.Normalize(productType))).ToList();
        }
        var candidates = await query.Take(200).ToListAsync(cancellationToken);
        IEnumerable<Product> ordered = candidates;
        if (budgetTarget.HasValue)
        {
            ordered = candidates
                .Where(x => (x.DiscountPrice ?? x.SalePrice ?? x.Price) >= budgetTarget.Value * 0.65m && (x.DiscountPrice ?? x.SalePrice ?? x.Price) <= budgetTarget.Value * 1.15m)
                .OrderByDescending(x => (x.DiscountPrice ?? x.SalePrice ?? x.Price) <= budgetTarget.Value)
                .ThenBy(x => Math.Abs((x.DiscountPrice ?? x.SalePrice ?? x.Price) - budgetTarget.Value));
            if (!ordered.Any()) ordered = candidates.OrderBy(x => Math.Abs((x.DiscountPrice ?? x.SalePrice ?? x.Price) - budgetTarget.Value));
        }
        else ordered = candidates.OrderBy(x => x.DiscountPrice ?? x.SalePrice ?? x.Price);
        return ordered.Take(max).Select(x => ToContext(x, scope == "PC" ? "PC" : scope == "ALL" ? (IsPcProduct(x) ? "PC" : ComponentTypes.Normalize(x.ComponentType)) : ComponentTypes.Normalize(productType))).ToList();
    }

    public async Task<IReadOnlyList<AiProductContext>> SearchAsync(string message, CancellationToken cancellationToken = default)
    {
        message = (message ?? string.Empty).Trim();
        var max = Math.Clamp(_options.MaxProductsContext, 1, 3);
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
        if (products.Count == 0 && analysis.CategoryScope == "PC" && !analysis.BudgetMin.HasValue && !analysis.BudgetMax.HasValue)
        {
            fallbackReason = "no_pc_in_exact_budget_or_keyword";
            products = await FindNearestPcAsync(inStockActiveQuery, analysis, max, cancellationToken);
        }

        if (products.Count == 0 && analysis.CategoryScope == "PC" && !analysis.BudgetMin.HasValue && !analysis.BudgetMax.HasValue)
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
            x.Id, x.Name, Price = x.DiscountPrice ?? x.SalePrice ?? x.Price, x.Specifications, x.ShortDescription, x.Description, x.WarrantyDuration, x.WarrantyMonths, x.StockQuantity, x.Slug, x.ThumbnailImage,
            Category = x.Category != null ? (x.Category.Name ?? "Chưa phân loại") : "Chưa phân loại"
        }).ToListAsync(ct);
        return rows.Select(x => new AiProductContext(
            x.Id,
            string.IsNullOrWhiteSpace(x.Name) ? "Sản phẩm không xác định" : x.Name,
            x.Price,
            TrimText(string.IsNullOrWhiteSpace(x.Specifications) ? x.ShortDescription ?? string.Empty : x.Specifications, 450),
            x.StockQuantity > 0 ? $"Còn hàng ({x.StockQuantity})" : "Tạm hết hàng",
            $"/Products/Detail/{x.Id}",
            string.IsNullOrWhiteSpace(x.Category) ? "Chưa phân loại" : x.Category,
            x.StockQuantity,
            scope, TrimText(x.Description ?? string.Empty, 300), !string.IsNullOrWhiteSpace(x.WarrantyDuration) ? x.WarrantyDuration : $"{x.WarrantyMonths} tháng", LabelForScope(scope), string.IsNullOrWhiteSpace(x.ThumbnailImage) ? "/images/no-image.png" : x.ThumbnailImage, scope.StartsWith("BUILD_", StringComparison.OrdinalIgnoreCase))).ToList();
    }


    private static IQueryable<Product> ApplyPcScope(IQueryable<Product> query) => query.Where(x =>
        x.ProductType == ProductKinds.PC ||
        x.ComponentType == "PC" ||
        (x.Category != null && (PcCategoryNames.Contains(x.Category.Name ?? string.Empty) || (x.Category.Name ?? string.Empty).Contains("PC") || (x.Category.Name ?? string.Empty).Contains("Cấu hình") || (x.Category.Name ?? string.Empty).Contains("Máy bộ") || (x.Category.Name ?? string.Empty).Contains("Máy tính"))));

    private static IQueryable<Product> ApplyComponentScope(IQueryable<Product> query, string? componentType)
    {
        query = query.Where(x => x.ProductType == ProductKinds.Component || (x.Category != null && ((x.Category.Name ?? string.Empty) == "Linh kiện" || (x.Category.Name ?? string.Empty) == "Màn hình" || (x.Category.Name ?? string.Empty).Contains("Phụ kiện") || (x.Category.Name ?? string.Empty).Contains("Bàn phím") || (x.Category.Name ?? string.Empty).Contains("Chuột") || (x.Category.Name ?? string.Empty).Contains("Tai nghe"))));
        if (string.IsNullOrWhiteSpace(componentType)) return query;
        var normalized = ComponentTypes.Normalize(componentType);
        return normalized switch
        {
            ComponentTypes.CPU => query.Where(x => x.ComponentType == ComponentTypes.CPU || (x.Category != null && (x.Category.Name ?? string.Empty).Contains("CPU")) || (x.Name ?? string.Empty).Contains("CPU") || (x.Name ?? string.Empty).Contains("Intel") || (x.Name ?? string.Empty).Contains("Ryzen")),
            ComponentTypes.Mainboard => query.Where(x => x.ComponentType == ComponentTypes.Mainboard || (x.Category != null && (x.Category.Name ?? string.Empty).Contains("Main")) || (x.Name ?? string.Empty).Contains("Main") || (x.Name ?? string.Empty).Contains("Bo mạch")),
            ComponentTypes.RAM => query.Where(x => x.ComponentType == ComponentTypes.RAM || (x.Category != null && (x.Category.Name ?? string.Empty).Contains("RAM")) || (x.Name ?? string.Empty).Contains("RAM")),
            ComponentTypes.VGA => query.Where(x => x.ComponentType == ComponentTypes.VGA || (x.Category != null && ((x.Category.Name ?? string.Empty).Contains("VGA") || (x.Category.Name ?? string.Empty).Contains("Card"))) || (x.Name ?? string.Empty).Contains("RTX") || (x.Name ?? string.Empty).Contains("GTX") || (x.Name ?? string.Empty).Contains("RX ") || (x.Name ?? string.Empty).Contains("VGA")),
            ComponentTypes.Storage => query.Where(x => x.ComponentType == ComponentTypes.Storage || (x.Category != null && ((x.Category.Name ?? string.Empty).Contains("SSD") || (x.Category.Name ?? string.Empty).Contains("HDD") || (x.Category.Name ?? string.Empty).Contains("Ổ cứng"))) || (x.Name ?? string.Empty).Contains("SSD") || (x.Name ?? string.Empty).Contains("HDD")),
            ComponentTypes.PSU => query.Where(x => x.ComponentType == ComponentTypes.PSU || (x.Category != null && ((x.Category.Name ?? string.Empty).Contains("Nguồn") || (x.Category.Name ?? string.Empty).Contains("PSU"))) || (x.Name ?? string.Empty).Contains("Nguồn") || (x.Name ?? string.Empty).Contains("PSU")),
            ComponentTypes.Case => query.Where(x => x.ComponentType == ComponentTypes.Case || (x.Category != null && ((x.Category.Name ?? string.Empty).Contains("Case") || (x.Category.Name ?? string.Empty).Contains("Vỏ"))) || (x.Name ?? string.Empty).Contains("Case") || (x.Name ?? string.Empty).Contains("Vỏ case")),
            ComponentTypes.Cooler => query.Where(x => x.ComponentType == ComponentTypes.Cooler || (x.Category != null && ((x.Category.Name ?? string.Empty).Contains("Tản") || (x.Category.Name ?? string.Empty).Contains("Cooler"))) || (x.Name ?? string.Empty).Contains("Tản") || (x.Name ?? string.Empty).Contains("Cooler")),
            ComponentTypes.Keyboard => query.Where(x => x.ComponentType == ComponentTypes.Keyboard || (x.Category != null && (x.Category.Name ?? string.Empty).Contains("Bàn phím")) || (x.Name ?? string.Empty).Contains("Bàn phím") || (x.Name ?? string.Empty).Contains("Keyboard") || (x.Name ?? string.Empty).Contains("Keycap")),
            ComponentTypes.Mouse => query.Where(x => x.ComponentType == ComponentTypes.Mouse || (x.Category != null && (x.Category.Name ?? string.Empty).Contains("Chuột")) || (x.Name ?? string.Empty).Contains("Chuột") || (x.Name ?? string.Empty).Contains("Mouse")),
            ComponentTypes.Headphone => query.Where(x => x.ComponentType == ComponentTypes.Headphone || (x.Category != null && (x.Category.Name ?? string.Empty).Contains("Tai nghe")) || (x.Name ?? string.Empty).Contains("Tai nghe") || (x.Name ?? string.Empty).Contains("Headset") || (x.Name ?? string.Empty).Contains("Headphone")),
            ComponentTypes.Monitor => query.Where(x => x.ComponentType == ComponentTypes.Monitor || (x.Category != null && ((x.Category.Name ?? string.Empty).Contains("Màn hình") || (x.Category.Name ?? string.Empty).Contains("Monitor"))) || (x.Name ?? string.Empty).Contains("Màn hình") || (x.Name ?? string.Empty).Contains("Monitor")),
            _ => query.Where(x => x.ComponentType == normalized)
        };
    }

    private static AiProductContext ToContext(Product x, string scope) => new(
        x.Id, string.IsNullOrWhiteSpace(x.Name) ? "Sản phẩm không xác định" : x.Name, x.DiscountPrice ?? x.SalePrice ?? x.Price,
        TrimText(string.IsNullOrWhiteSpace(x.Specifications) ? x.ShortDescription ?? string.Empty : x.Specifications, 450),
        x.StockQuantity > 0 ? $"Còn hàng ({x.StockQuantity})" : "Tạm hết hàng",
        $"/Products/Detail/{x.Id}", x.Category?.Name ?? "Chưa phân loại", x.StockQuantity, scope,
        TrimText(x.Description ?? string.Empty, 300), !string.IsNullOrWhiteSpace(x.WarrantyDuration) ? x.WarrantyDuration : $"{x.WarrantyMonths} tháng", LabelForScope(scope, x), string.IsNullOrWhiteSpace(x.ThumbnailImage) ? "/images/no-image.png" : x.ThumbnailImage, scope.StartsWith("BUILD_", StringComparison.OrdinalIgnoreCase));

    private void LogSearch(string message, AiSearchAnalysis analysis, IReadOnlyList<AiProductContext> products, int activeCount, int scopedCount, int budgetCount, string fallbackReason)
    {
        _logger.LogInformation("[AI_PRODUCT_SEARCH] message={Message}; intent={Intent}; budgetMin={BudgetMin}; budgetMax={BudgetMax}; keywordGame={TargetGame}; componentType={ComponentType}; requestedProductType={RequestedProductType}; excludedTypes={ExcludedTypes}; categoryScope={Scope}; activeStockProducts={ActiveCount}; scopedProducts={ScopedCount}; budgetProducts={BudgetCount}; finalProducts={Products}; fallbackReason={FallbackReason}; topProducts={TopProducts}",
            TrimText(message, 180), analysis.Intent, analysis.BudgetMin, analysis.BudgetMax, analysis.TargetGame ?? "none", analysis.ComponentType ?? "none", analysis.CategoryScope == "PC" ? "PC/build" : analysis.ComponentType ?? "product", analysis.CategoryScope == "PC" ? string.Join(",", AccessoryTypes) : "none", analysis.CategoryScope, activeCount, scopedCount, budgetCount, products.Count, fallbackReason, string.Join(" | ", products.Take(5).Select(p => $"{p.Id}:{p.Name}:{p.Price:N0}:{p.Category}:{p.CategoryScope}:{p.ProductTypeLabel}")));
    }

    private static int ScoreProduct(Product product, IReadOnlyList<string> tokens, AiSearchAnalysis analysis)
    {
        var haystack = $"{product.Name ?? string.Empty} {product.ShortDescription ?? string.Empty} {product.Description ?? string.Empty} {product.Specifications ?? string.Empty} {product.Category?.Name ?? string.Empty}";
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
        var hasSpecificProductName = DetectSpecificProductName(normalized, componentType);
        var isProductQuestion = IsProductQuestion(normalized);
        var isPcAdvice = !hasSpecificProductName && !isProductQuestion && (PcIntentWords.Any(normalized.Contains) || !string.IsNullOrWhiteSpace(game)) && accessory == null && componentType == null;
        var isPolicy = ContainsAny(normalized, "tra gop", "freeship", "doi tra", "bao hanh tan noi", "hoa toc", "chinh sach");
        var isOrder = ContainsAny(normalized, "don hang", "ma don", "dh0");
        var isWarranty = ContainsAny(normalized, "bao hanh", "warranty");
        var isPayment = ContainsAny(normalized, "thanh toan", "chuyen khoan", "cod");
        var isHuman = ContainsAny(normalized, "nhan vien", "tu van vien", "nguoi that");
        var intent = isHuman ? "HUMAN_SUPPORT" : isOrder ? "ORDER_QA" : isPayment ? "PAYMENT_QA" : isWarranty ? "WARRANTY_QA" : isPolicy ? "POLICY_QA" : isPcAdvice ? "PC_ADVICE" : isCompare ? "PRODUCT_COMPARE" : "PRODUCT_QA";
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
        if (prefix.Contains("duoi") || prefix.Contains("toi da") || prefix.Contains("<=") || prefix.Contains("<")) return (null, amount);
        if (prefix.Contains("tren") || prefix.Contains(">=") || prefix.Contains(">")) return (amount, null);
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

    private static bool IsProductQuestion(string normalized)
        => ContainsAny(normalized, "ton dien", "hao dien", "an dien", "dien nang", "cong suat", "nong khong", "on khong", "co tot khong", "tot khong", "co nen mua", "uu diem", "nhuoc diem", "danh gia", "phan tich", "choi duoc khong", "nang cap duoc khong");

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

    [GeneratedRegex(@"(duoi|toi da|khoang|tam|tren|>=|>|<=|<)?\s*(\d+(?:[\.,]\d+)?)\s*(?:trieu|tr|m|million)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
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
    private static readonly string[] PcIntentWords = ["cau hinh", "pc", "may tinh", "may bo", "may choi game", "choi game", "gaming", "fps", "setting", "build", "stream", "render", "do hoa", "valorant", "cs2", "lol", "pubg", "gta", "gta5", "gta v", "black myth", "ngan sach", "trieu", "shop co san"];
    private static readonly Dictionary<string, string> GameWords = new(StringComparer.OrdinalIgnoreCase) { ["valorant"] = "Valorant", ["cs2"] = "CS2", ["lol"] = "LOL", ["pubg"] = "PUBG", ["gta5"] = "GTA V", ["gta v"] = "GTA V", ["gta"] = "GTA V", ["black myth"] = "Black Myth" };
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
        [ComponentTypes.RAM] = [" ram", "ram ", "thanh ram", "bo nho"],
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
        ComponentTypes.CPU => "CPU đề xuất",
        ComponentTypes.VGA => "VGA đề xuất",
        ComponentTypes.Mainboard => "Mainboard đề xuất",
        ComponentTypes.RAM => "RAM đề xuất",
        ComponentTypes.Storage => "SSD/HDD đề xuất",
        ComponentTypes.PSU => "Nguồn đề xuất",
        ComponentTypes.Case => "Vỏ case đề xuất",
        ComponentTypes.Cooler => "Tản nhiệt đề xuất",
        ComponentTypes.Mouse => "Chuột đề xuất",
        ComponentTypes.Keyboard => "Bàn phím đề xuất",
        ComponentTypes.Headphone => "Tai nghe đề xuất",
        ComponentTypes.Monitor => "Màn hình đề xuất",
        _ => "Linh kiện đề xuất"
    };

    private async Task<List<AiProductContext>> FindNearestPcAsync(IQueryable<Product> inStockActiveQuery, AiSearchAnalysis analysis, int max, CancellationToken ct)
    {
        var target = analysis.BudgetMax ?? analysis.BudgetMin;
        var candidates = (await ApplyPcScope(inStockActiveQuery).Take(200).ToListAsync(ct))
            .Where(x => !IsAccessoryProduct(x))
            .ToList();
        var ordered = target.HasValue
            ? candidates.Where(x => (x.DiscountPrice ?? x.SalePrice ?? x.Price) >= target.Value * 0.7m && (x.DiscountPrice ?? x.SalePrice ?? x.Price) <= target.Value * 1.15m)
                .OrderByDescending(x => x.StockQuantity > 0)
                .ThenBy(x => Math.Abs((x.DiscountPrice ?? x.SalePrice ?? x.Price) - target.Value))
            : candidates.OrderByDescending(x => x.StockQuantity > 0).ThenBy(x => x.DiscountPrice ?? x.SalePrice ?? x.Price);
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
