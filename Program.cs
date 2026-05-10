using Datn.PcStore.Data;
using Datn.PcStore.Services;
using Datn.PcStore.Constants;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
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
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddSession();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IProductImageStorageService, ProductImageStorageService>();
builder.Services.AddHttpClient<IMapProvider, OpenStreetMapProvider>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("DATN-PC-Store/1.0");
});
builder.Services.AddScoped<IGeocodingService, GeocodingService>();
builder.Services.AddScoped<IRouteService, RouteService>();
builder.Services.AddScoped<IShippingFeeCalculator, ShippingFeeCalculator>();
builder.Services.AddScoped<IShippingService, ShippingService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();

    await db.Database.ExecuteSqlRawAsync(@"IF COL_LENGTH('SiteSettings', 'DealSectionBackgroundUrl') IS NULL
BEGIN
    ALTER TABLE SiteSettings ADD DealSectionBackgroundUrl NVARCHAR(1000) NULL;
END");

    await db.Database.ExecuteSqlRawAsync(@"IF COL_LENGTH('SiteSettings', 'HotPromotionBackgroundUrl') IS NULL
BEGIN
    ALTER TABLE SiteSettings ADD HotPromotionBackgroundUrl NVARCHAR(1000) NULL;
END");



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
        var point = await mapProvider.GeocodeAsync(shopAddress) ?? new GeoPoint(21.012763, 105.821866);
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

    await SeedData.InitializeAsync(db);
}

app.Run();
