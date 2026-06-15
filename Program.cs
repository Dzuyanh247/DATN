using Datn.PcStore.Data;
using Datn.PcStore.Services;
using Datn.PcStore.Constants;
using Datn.PcStore.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = AuthSchemes.PcStoreCookie;
    options.DefaultChallengeScheme = AuthSchemes.PcStoreCookie;
    options.DefaultSignInScheme = AuthSchemes.PcStoreCookie;
    options.DefaultSignOutScheme = AuthSchemes.PcStoreCookie;
})
    .AddCookie(AuthSchemes.PcStoreCookie, options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Events.OnRedirectToLogin = async context =>
        {
            if (ExpectsJson(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { success = false, message = "Vui lòng đăng nhập để tiếp tục.", data = (object?)null });
                return;
            }

            context.Response.Redirect(context.RedirectUri);
        };
        options.Events.OnRedirectToAccessDenied = async context =>
        {
            if (ExpectsJson(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { success = false, message = "Bạn không có quyền thực hiện thao tác này.", data = (object?)null });
                return;
            }

            context.Response.Redirect(context.RedirectUri);
        };
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.Configure<GhnOptions>(builder.Configuration.GetSection("GHN"));
builder.Services.Configure<ShippingPolicyOptions>(builder.Configuration.GetSection("ShippingPolicy"));
builder.Services.Configure<ShopAddressOptions>(builder.Configuration.GetSection("ShopAddress"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
});
builder.Services.AddSession();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IAccountPasswordResetService, AccountPasswordResetService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICompareService, CompareSessionService>();
builder.Services.AddScoped<BuildCompatibilityService>();
builder.Services.AddScoped<IProductImageStorageService, ProductImageStorageService>();
builder.Services.AddHttpClient<IMapProvider, OpenRouteServiceProvider>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);
});
builder.Services.AddScoped<IGeocodingService, GeocodingService>();
builder.Services.AddScoped<IRouteService, RouteService>();
builder.Services.AddScoped<IShippingFeeCalculator, ShippingFeeCalculator>();
builder.Services.AddScoped<IShippingService, ShippingService>();
builder.Services.AddScoped<IOrderExpirationService, OrderExpirationService>();
builder.Services.AddScoped<IProductReviewService, ProductReviewService>();
builder.Services.AddHttpClient<IGhnShippingService, GhnShippingService>((sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GhnOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient<IGhnAddressService, GhnAddressService>((sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GhnOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
    if (!string.IsNullOrWhiteSpace(options.Token))
    {
        client.DefaultRequestHeaders.Remove("Token");
        client.DefaultRequestHeaders.Add("Token", options.Token);
    }
});


var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
var csBuilder = new SqlConnectionStringBuilder(defaultConnection);
Console.WriteLine($"[DB] Environment: {builder.Environment.EnvironmentName}; Server: {csBuilder.DataSource}; Database: {csBuilder.InitialCatalog}");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.Use(async (context, next) =>
{
    try
    {
        await next();

        if (IsChatRequest(context.Request) &&
            context.Response.StatusCode == StatusCodes.Status400BadRequest &&
            !context.Response.HasStarted &&
            (string.IsNullOrWhiteSpace(context.Response.ContentType) ||
             !context.Response.ContentType.Contains("application/json", StringComparison.OrdinalIgnoreCase)))
        {
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Yêu cầu chat không hợp lệ hoặc mã bảo mật đã hết hạn. Vui lòng tải lại trang và thử lại.",
                data = (object?)null
            });
        }
    }
    catch (Exception exception) when (IsChatRequest(context.Request) && !context.Response.HasStarted)
    {
        app.Logger.LogError(exception, "Unhandled chat request error for {Method} {Path}", context.Request.Method, context.Request.Path);
        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            message = "Hệ thống chat đang gặp sự cố. Vui lòng thử lại sau.",
            data = (object?)null
        });
    }
});

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<ChatHub>("/hubs/support-chat");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    var currentDatabaseName = await db.Database.SqlQueryRaw<string>("SELECT DB_NAME() AS [Value]").SingleAsync();
    Console.WriteLine($"[DB] SQL DB_NAME(): {currentDatabaseName}");

    await EnsureProductPromotionColumnsAsync(db);

    await db.Database.ExecuteSqlRawAsync(@"IF COL_LENGTH('SiteSettings', 'DealSectionBackgroundUrl') IS NULL
BEGIN
    ALTER TABLE SiteSettings ADD DealSectionBackgroundUrl NVARCHAR(1000) NULL;
END");

    await db.Database.ExecuteSqlRawAsync(@"IF COL_LENGTH('SiteSettings', 'HotPromotionBackgroundUrl') IS NULL
BEGIN
    ALTER TABLE SiteSettings ADD HotPromotionBackgroundUrl NVARCHAR(1000) NULL;
END");

    await db.Database.ExecuteSqlRawAsync(@"IF COL_LENGTH('Orders', 'PaymentExpireAt') IS NULL
BEGIN
    ALTER TABLE Orders ADD PaymentExpireAt datetime2 NULL;
END");




    var auditTables = new[]
    {
        "Banners",
        "Categories",
        "Products",
        "Articles",
        "SiteSettings",
        "Orders",
        "ShopLocations",
        "ShippingConfigs"
    };

    foreach (var tableName in auditTables)
    {
        await db.Database.ExecuteSqlRawAsync($@"IF OBJECT_ID('{tableName}', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('{tableName}', 'CreatedAt') IS NOT NULL
    BEGIN
        UPDATE [{tableName}] SET [CreatedAt] = GETUTCDATE() WHERE [CreatedAt] IS NULL;

        IF EXISTS (
            SELECT 1
            FROM sys.columns c
            JOIN sys.tables t ON t.object_id = c.object_id
            JOIN sys.types ty ON ty.user_type_id = c.user_type_id
            WHERE t.name = '{tableName}'
              AND c.name = 'CreatedAt'
              AND c.is_nullable = 1
              AND ty.name = 'datetime2'
        )
        BEGIN
            IF EXISTS (
                SELECT 1
                FROM sys.default_constraints dc
                JOIN sys.columns c ON c.default_object_id = dc.object_id
                JOIN sys.tables t ON t.object_id = c.object_id
                WHERE t.name = '{tableName}' AND c.name = 'CreatedAt'
            )
            BEGIN
                DECLARE @SqlDropCreatedAt NVARCHAR(MAX);
                SELECT @SqlDropCreatedAt = 'ALTER TABLE [{tableName}] DROP CONSTRAINT ' + QUOTENAME(dc.name)
                FROM sys.default_constraints dc
                JOIN sys.columns c ON c.default_object_id = dc.object_id
                JOIN sys.tables t ON t.object_id = c.object_id
                WHERE t.name = '{tableName}' AND c.name = 'CreatedAt';
                EXEC sp_executesql @SqlDropCreatedAt;
            END

            ALTER TABLE [{tableName}] ALTER COLUMN [CreatedAt] DATETIME2 NOT NULL;
            ALTER TABLE [{tableName}] ADD CONSTRAINT [DF_{tableName}_CreatedAt] DEFAULT GETUTCDATE() FOR [CreatedAt];
        END
    END

    IF COL_LENGTH('{tableName}', 'UpdatedAt') IS NOT NULL
    BEGIN
        UPDATE [{tableName}] SET [UpdatedAt] = GETUTCDATE() WHERE [UpdatedAt] IS NULL;

        IF EXISTS (
            SELECT 1
            FROM sys.columns c
            JOIN sys.tables t ON t.object_id = c.object_id
            JOIN sys.types ty ON ty.user_type_id = c.user_type_id
            WHERE t.name = '{tableName}'
              AND c.name = 'UpdatedAt'
              AND c.is_nullable = 1
              AND ty.name = 'datetime2'
        )
        BEGIN
            IF EXISTS (
                SELECT 1
                FROM sys.default_constraints dc
                JOIN sys.columns c ON c.default_object_id = dc.object_id
                JOIN sys.tables t ON t.object_id = c.object_id
                WHERE t.name = '{tableName}' AND c.name = 'UpdatedAt'
            )
            BEGIN
                DECLARE @SqlDropUpdatedAt NVARCHAR(MAX);
                SELECT @SqlDropUpdatedAt = 'ALTER TABLE [{tableName}] DROP CONSTRAINT ' + QUOTENAME(dc.name)
                FROM sys.default_constraints dc
                JOIN sys.columns c ON c.default_object_id = dc.object_id
                JOIN sys.tables t ON t.object_id = c.object_id
                WHERE t.name = '{tableName}' AND c.name = 'UpdatedAt';
                EXEC sp_executesql @SqlDropUpdatedAt;
            END

            ALTER TABLE [{tableName}] ALTER COLUMN [UpdatedAt] DATETIME2 NOT NULL;
            ALTER TABLE [{tableName}] ADD CONSTRAINT [DF_{tableName}_UpdatedAt] DEFAULT GETUTCDATE() FOR [UpdatedAt];
        END
    END
END");
    }
    await db.Database.ExecuteSqlRawAsync(@"IF OBJECT_ID('ShippingConfigs', 'U') IS NULL
BEGIN
    CREATE TABLE ShippingConfigs (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        BaseDistanceKm DECIMAL(8,2) NOT NULL DEFAULT 3,
        BaseFee DECIMAL(18,2) NOT NULL DEFAULT 15000,
        ExtraFeePerKm DECIMAL(18,2) NOT NULL DEFAULT 5000,
        MaxDistanceKm DECIMAL(8,2) NOT NULL DEFAULT 15,
        FreeShippingDistanceKm DECIMAL(8,2) NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END");

    await db.Database.ExecuteSqlRawAsync(@"IF OBJECT_ID('ShopLocations', 'U') IS NULL
BEGIN
    CREATE TABLE ShopLocations (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ShopName NVARCHAR(120) NOT NULL,
        Address NVARCHAR(250) NOT NULL,
        Latitude FLOAT NOT NULL,
        Longitude FLOAT NOT NULL,
        IsDefault BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END");

    await db.Database.ExecuteSqlRawAsync(@"IF COL_LENGTH('Orders', 'ShippingDistanceKm') IS NULL
BEGIN
    ALTER TABLE Orders ADD ShippingDistanceKm FLOAT NOT NULL DEFAULT 0;
END");
    await db.Database.ExecuteSqlRawAsync(@"IF COL_LENGTH('Orders', 'ShippingDurationMinutes') IS NULL
BEGIN
    ALTER TABLE Orders ADD ShippingDurationMinutes INT NOT NULL DEFAULT 0;
END");
    await db.Database.ExecuteSqlRawAsync(@"IF COL_LENGTH('Orders', 'ShippingFee') IS NULL
BEGIN
    ALTER TABLE Orders ADD ShippingFee DECIMAL(18,2) NOT NULL DEFAULT 0;
END");
    await db.Database.ExecuteSqlRawAsync(@"IF COL_LENGTH('Orders', 'ShippingProvider') IS NULL
BEGIN
    ALTER TABLE Orders ADD ShippingProvider NVARCHAR(100) NULL;
END");
    await db.Database.ExecuteSqlRawAsync(@"IF COL_LENGTH('Orders', 'ShippingFormulaSnapshot') IS NULL
BEGIN
    ALTER TABLE Orders ADD ShippingFormulaSnapshot NVARCHAR(300) NULL;
END");

    await db.Database.ExecuteSqlRawAsync(@"IF COL_LENGTH('ShippingConfigs', 'FreeShippingDistanceKm') IS NULL
BEGIN
    ALTER TABLE ShippingConfigs ADD FreeShippingDistanceKm DECIMAL(8,2) NOT NULL DEFAULT 0;
END");
    await db.Database.ExecuteSqlRawAsync(@"IF COL_LENGTH('ShippingConfigs', 'IsActive') IS NULL
BEGIN
    ALTER TABLE ShippingConfigs ADD IsActive BIT NOT NULL DEFAULT 1;
END");
    await db.Database.ExecuteSqlRawAsync(@"IF COL_LENGTH('ShopLocations', 'IsDefault') IS NULL
BEGIN
    ALTER TABLE ShopLocations ADD IsDefault BIT NOT NULL DEFAULT 1;
END");

    await db.Database.ExecuteSqlRawAsync(@"IF NOT EXISTS (SELECT 1 FROM ShippingConfigs)
BEGIN
    INSERT INTO ShippingConfigs(BaseDistanceKm, BaseFee, ExtraFeePerKm, MaxDistanceKm, FreeShippingDistanceKm, IsActive, CreatedAt, UpdatedAt)
    VALUES (3,15000,5000,15,2,1,SYSUTCDATETIME(),SYSUTCDATETIME());
END");

    var mapProvider = scope.ServiceProvider.GetRequiredService<IMapProvider>();
    if (!await db.ShopLocations.AnyAsync())
    {
        const string shopAddress = "83-85 Thái Hà, Đống Đa, Hà Nội";
        var point = await mapProvider.GeocodeAsync(shopAddress, "Thành phố Hà Nội") ?? new GeoPoint(21.012763, 105.821866);
        db.ShopLocations.Add(new Datn.PcStore.Models.ShopLocation
        {
            ShopName = "Cửa hàng chính",
            Address = shopAddress,
            Latitude = point.Latitude,
            Longitude = point.Longitude,
            IsDefault = true
        });
        await db.SaveChangesAsync();
    }


    var defaultShop = await db.ShopLocations.FirstOrDefaultAsync(x => x.IsDefault);
    if (defaultShop != null)
    {
        var lat = defaultShop.Latitude;
        var lng = defaultShop.Longitude;
        if (Math.Abs(lat) > 90 && Math.Abs(lat) <= 9000000000d) lat /= 10000000d;
        if (Math.Abs(lng) > 180 && Math.Abs(lng) <= 18000000000d) lng /= 10000000d;
        if (lat != defaultShop.Latitude || lng != defaultShop.Longitude)
        {
            defaultShop.Latitude = lat;
            defaultShop.Longitude = lng;
            await db.SaveChangesAsync();
        }
    }
    await SeedData.InitializeAsync(db);
}

app.Run();

static async Task EnsureProductPromotionColumnsAsync(ApplicationDbContext db)
{
    await db.Database.ExecuteSqlRawAsync(@"IF COL_LENGTH('Products', 'IsHotSale') IS NULL
BEGIN
    ALTER TABLE Products ADD IsHotSale bit NOT NULL CONSTRAINT DF_Products_IsHotSale DEFAULT(0);
END

IF COL_LENGTH('Products', 'IsDailyDeal') IS NULL
BEGIN
    ALTER TABLE Products ADD IsDailyDeal bit NOT NULL CONSTRAINT DF_Products_IsDailyDeal DEFAULT(0);
END

IF COL_LENGTH('Products', 'IsPromotion') IS NULL
BEGIN
    ALTER TABLE Products ADD IsPromotion bit NOT NULL CONSTRAINT DF_Products_IsPromotion DEFAULT(0);
END

IF COL_LENGTH('Products', 'PromotionStartDate') IS NULL
BEGIN
    ALTER TABLE Products ADD PromotionStartDate datetime2 NULL;
END

IF COL_LENGTH('Products', 'PromotionEndDate') IS NULL
BEGIN
    ALTER TABLE Products ADD PromotionEndDate datetime2 NULL;
END");

    var promotionColumns = await db.Database.SqlQueryRaw<string>(@"SELECT c.name AS [Value]
FROM sys.columns c
INNER JOIN sys.tables t ON t.object_id = c.object_id
WHERE t.name = 'Products'
  AND c.name IN ('IsHotSale', 'IsDailyDeal', 'IsPromotion', 'PromotionStartDate', 'PromotionEndDate')
ORDER BY c.name;").ToListAsync();
    Console.WriteLine($"[DB] Products promotion columns present: {string.Join(", ", promotionColumns)}");
}


static bool ExpectsJson(HttpRequest request) =>
    IsChatRequest(request) && request.Headers.Accept.Any(x =>
        x?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);

static bool IsChatRequest(HttpRequest request) =>
    request.Path.StartsWithSegments("/support-chat", StringComparison.OrdinalIgnoreCase) ||
    request.Path.StartsWithSegments("/AdminChat", StringComparison.OrdinalIgnoreCase);
