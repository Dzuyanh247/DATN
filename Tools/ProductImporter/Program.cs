using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Datn.PcStore.Data;
using Datn.PcStore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

const string placeholderImage = "https://via.placeholder.com/600x600?text=No+Image";

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddJsonFile("appsettings.ProductImporter.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = config.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");

var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer(connectionString)
    .Options;

using var db = new ApplicationDbContext(options);

var source = config["Importer:Source"] ?? "ttgshop";
var mode = config["Importer:Mode"] ?? "auto";
var jsonPath = config["Importer:JsonPath"];
var csvPath = config["Importer:CsvPath"];

Console.WriteLine($"[Importer] Source: {source}");
Console.WriteLine($"[Importer] Mode: {mode}");

var categories = new[]
{
    new CategoryMap("CPU", "https://ttgshop.vn/cpu-bo-vi-xu-ly"),
    new CategoryMap("Mainboard", "https://ttgshop.vn/mainboard-bo-mach-chu"),
    new CategoryMap("RAM", "https://ttgshop.vn/ram-may-tinh"),
    new CategoryMap("VGA", "https://ttgshop.vn/card-man-hinh"),
    new CategoryMap("SSD/HDD", "https://ttgshop.vn/o-cung-ssd"),
    new CategoryMap("PSU", "https://ttgshop.vn/nguon-may-tinh"),
    new CategoryMap("Tản nhiệt", "https://ttgshop.vn/tan-nhiet-cpu"),
    new CategoryMap("Case", "https://ttgshop.vn/case-vo-may-tinh"),
    new CategoryMap("Màn hình", "https://ttgshop.vn/man-hinh-may-tinh")
};

var imported = new List<ImportProduct>();
if (mode.Equals("auto", StringComparison.OrdinalIgnoreCase) || mode.Equals("api", StringComparison.OrdinalIgnoreCase))
{
    var apiItems = await TryImportFromHuraApi(config);
    imported.AddRange(apiItems);
}

if ((mode.Equals("auto", StringComparison.OrdinalIgnoreCase) || mode.Equals("html", StringComparison.OrdinalIgnoreCase)) && imported.Count == 0)
{
    var htmlItems = await ImportFromHtmlCategories(categories);
    imported.AddRange(htmlItems);
}

if ((mode.Equals("auto", StringComparison.OrdinalIgnoreCase) || mode.Equals("json", StringComparison.OrdinalIgnoreCase)) && imported.Count == 0 && !string.IsNullOrWhiteSpace(jsonPath) && File.Exists(jsonPath))
{
    Console.WriteLine($"[Importer] Fallback JSON: {jsonPath}");
    imported.AddRange(ReadJson(jsonPath));
}

if ((mode.Equals("auto", StringComparison.OrdinalIgnoreCase) || mode.Equals("csv", StringComparison.OrdinalIgnoreCase)) && imported.Count == 0 && !string.IsNullOrWhiteSpace(csvPath) && File.Exists(csvPath))
{
    Console.WriteLine($"[Importer] Fallback CSV: {csvPath}");
    imported.AddRange(ReadCsv(csvPath));
}

if (imported.Count == 0)
{
    Console.WriteLine("[Importer] No products found from all sources.");
    return;
}

var stats = await UpsertProducts(db, imported);
Console.WriteLine($"[Importer] Total found: {imported.Count}");
Console.WriteLine($"[Importer] Insert: {stats.Inserted}");
Console.WriteLine($"[Importer] Update: {stats.Updated}");

static async Task<List<ImportProduct>> TryImportFromHuraApi(IConfiguration config)
{
    var result = new List<ImportProduct>();
    var endpoint = config["Importer:HuraApiEndpoint"];
    if (string.IsNullOrWhiteSpace(endpoint))
    {
        Console.WriteLine("[Importer] Hura API endpoint is empty -> skip API");
        return result;
    }

    using var http = new HttpClient();
    Console.WriteLine($"[Importer] Trying Hura API: {endpoint}");

    try
    {
        var response = await http.GetAsync(endpoint);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode || body.Contains("You need to use Hura.Ajax.post/get", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[Importer] API blocked or failed ('You need to use Hura.Ajax.post/get'). Switch to HTML crawl.");
            return result;
        }

        using var doc = JsonDocument.Parse(body);
        var products = doc.RootElement.TryGetProperty("products", out var arr) && arr.ValueKind == JsonValueKind.Array ? arr : default;
        if (products.ValueKind != JsonValueKind.Array) return result;

        foreach (var item in products.EnumerateArray())
        {
            result.Add(new ImportProduct
            {
                Name = item.GetPropertyOrDefault("name"),
                Price = ParsePrice(item.GetPropertyOrDefault("price")),
                ImageUrl = item.GetPropertyOrDefault("image"),
                ProductUrl = item.GetPropertyOrDefault("url"),
                Category = item.GetPropertyOrDefault("category"),
                Brand = item.GetPropertyOrDefault("brand"),
                Warranty = item.GetPropertyOrDefault("warranty"),
                Stock = ParseInt(item.GetPropertyOrDefault("stock"), 0),
                Description = item.GetPropertyOrDefault("description")
            });
        }
        Console.WriteLine($"[Importer] API fetched {result.Count} products");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Importer] API error: {ex.Message}");
    }

    return result;
}

static async Task<List<ImportProduct>> ImportFromHtmlCategories(IEnumerable<CategoryMap> categories)
{
    var all = new List<ImportProduct>();
    using var http = new HttpClient();

    foreach (var cat in categories)
    {
        Console.WriteLine($"[Importer] Crawling category: {cat.Name} - {cat.Url}");
        try
        {
            var html = await http.GetStringAsync(cat.Url);
            var products = ParseProductsFromHtml(html, cat.Name, cat.Url);
            Console.WriteLine($"[Importer] Found {products.Count} products in {cat.Name}");
            all.AddRange(products);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Importer] Crawl error category {cat.Name}: {ex.Message}");
        }
    }

    return all;
}

static List<ImportProduct> ParseProductsFromHtml(string html, string categoryName, string baseUrl)
{
    var result = new List<ImportProduct>();
    var cardRegex = new Regex("<a[^>]*class=\\\"[^\\\"]*p-item[^\\\"]*\\\"[^>]*>(.*?)</a>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
    var matches = cardRegex.Matches(html);

    foreach (Match m in matches)
    {
        var card = m.Value;
        var name = Extract(card, "title=\\\"", "\\\"") ?? Extract(card, "alt=\\\"", "\\\"") ?? "Unknown";
        var href = Extract(card, "href=\\\"", "\\\"") ?? string.Empty;
        var img = Extract(card, "src=\\\"", "\\\"") ?? string.Empty;
        var priceText = Extract(card, "class=\\\"p-price\\\"", "</") ?? string.Empty;
        var price = ParsePrice(priceText);

        result.Add(new ImportProduct
        {
            Name = WebUtility(name),
            Price = price,
            ImageUrl = NormalizeUrl(img, baseUrl),
            ProductUrl = NormalizeUrl(href, baseUrl),
            Category = categoryName,
            Brand = GuessBrand(name),
            Warranty = "12 tháng",
            Stock = 0,
            Description = string.Empty
        });
    }

    return result;
}

static async Task<(int Inserted, int Updated)> UpsertProducts(ApplicationDbContext db, List<ImportProduct> products)
{
    var inserted = 0;
    var updated = 0;

    foreach (var group in products.Where(x => !string.IsNullOrWhiteSpace(x.Name)).GroupBy(x => x.Category))
    {
        var categoryName = string.IsNullOrWhiteSpace(group.Key) ? "Khác" : group.Key.Trim();
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
        if (category == null)
        {
            category = new Category { Name = categoryName };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
        }

        foreach (var item in group)
        {
            var url = item.ProductUrl?.Trim() ?? string.Empty;
            var name = item.Name.Trim();
            var existing = await db.Products.FirstOrDefaultAsync(p =>
                (!string.IsNullOrWhiteSpace(url) && p.SourceUrl == url) || p.Name == name);

            if (existing == null)
            {
                db.Products.Add(new Product
                {
                    Name = name,
                    Slug = Slugify(name),
                    ProductCode = $"IMP-{Guid.NewGuid():N}"[..18],
                    Brand = item.Brand ?? string.Empty,
                    Price = item.Price,
                    SalePrice = item.Price,
                    DiscountPrice = item.Price,
                    StockQuantity = Math.Max(item.Stock, 0),
                    ThumbnailImage = string.IsNullOrWhiteSpace(item.ImageUrl) ? placeholderImage : item.ImageUrl!,
                    ShortDescription = Truncate(item.Description, 500),
                    Description = item.Description ?? string.Empty,
                    DetailDescription = item.Description ?? string.Empty,
                    WarrantyDuration = string.IsNullOrWhiteSpace(item.Warranty) ? "12 tháng" : item.Warranty!,
                    IsInStock = item.Stock > 0,
                    ComponentType = categoryName,
                    CategoryId = category.Id,
                    SourceUrl = url
                });
                inserted++;
            }
            else
            {
                existing.Price = item.Price;
                existing.SalePrice = item.Price;
                existing.DiscountPrice = item.Price;
                existing.StockQuantity = Math.Max(item.Stock, 0);
                existing.IsInStock = item.Stock > 0;
                existing.ThumbnailImage = string.IsNullOrWhiteSpace(item.ImageUrl) ? existing.ThumbnailImage : item.ImageUrl;
                existing.Brand = string.IsNullOrWhiteSpace(item.Brand) ? existing.Brand : item.Brand!;
                existing.Description = string.IsNullOrWhiteSpace(item.Description) ? existing.Description : item.Description!;
                existing.WarrantyDuration = string.IsNullOrWhiteSpace(item.Warranty) ? existing.WarrantyDuration : item.Warranty!;
                existing.ComponentType = categoryName;
                existing.CategoryId = category.Id;
                if (!string.IsNullOrWhiteSpace(url)) existing.SourceUrl = url;
                updated++;
            }
        }
    }

    await db.SaveChangesAsync();
    return (inserted, updated);
}

static string Slugify(string value)
{
    var cleaned = Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9\\s-]", "");
    cleaned = Regex.Replace(cleaned, "\\s+", "-").Trim('-');
    return string.IsNullOrWhiteSpace(cleaned) ? $"product-{Guid.NewGuid():N}"[..20] : cleaned[..Math.Min(200, cleaned.Length)];
}

static decimal ParsePrice(string? input)
{
    if (string.IsNullOrWhiteSpace(input)) return 0;
    var digits = Regex.Replace(input, "[^0-9]", "");
    return decimal.TryParse(digits, NumberStyles.Any, CultureInfo.InvariantCulture, out var val) ? val : 0;
}

static int ParseInt(string? input, int d) => int.TryParse(input, out var v) ? v : d;
static string WebUtility(string input) => System.Net.WebUtility.HtmlDecode(input).Trim();
static string Truncate(string? input, int len) => string.IsNullOrWhiteSpace(input) ? string.Empty : input.Length <= len ? input : input[..len];
static string? GuessBrand(string? name) => string.IsNullOrWhiteSpace(name) ? string.Empty : name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

static string NormalizeUrl(string input, string baseUrl)
{
    if (string.IsNullOrWhiteSpace(input)) return string.Empty;
    if (Uri.TryCreate(input, UriKind.Absolute, out var abs)) return abs.ToString();
    return new Uri(new Uri(baseUrl), input).ToString();
}

static string? Extract(string input, string start, string end)
{
    var i = input.IndexOf(start, StringComparison.OrdinalIgnoreCase);
    if (i < 0) return null;
    i += start.Length;
    var j = input.IndexOf(end, i, StringComparison.OrdinalIgnoreCase);
    return j > i ? input[i..j] : null;
}

static List<ImportProduct> ReadJson(string path)
{
    var raw = File.ReadAllText(path);
    return JsonSerializer.Deserialize<List<ImportProduct>>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
}

static List<ImportProduct> ReadCsv(string path)
{
    var rows = File.ReadAllLines(path);
    var list = new List<ImportProduct>();
    foreach (var row in rows.Skip(1))
    {
        var c = row.Split(',');
        if (c.Length < 9) continue;
        list.Add(new ImportProduct(c[0], ParsePrice(c[1]), c[2], c[3], c[4], c[5], c[6], ParseInt(c[7], 0), c[8]));
    }
    return list;
}

internal sealed record CategoryMap(string Name, string Url);
internal sealed record ImportProduct(
    string Name,
    decimal Price,
    string? ImageUrl,
    string? ProductUrl,
    string Category,
    string? Brand,
    string? Warranty,
    int Stock,
    string? Description)
{
    public ImportProduct() : this(string.Empty, 0, null, null, string.Empty, null, null, 0, null) { }
}

internal static class JsonExtensions
{
    public static string GetPropertyOrDefault(this JsonElement element, string name)
        => element.TryGetProperty(name, out var val) ? val.ToString() : string.Empty;
}
