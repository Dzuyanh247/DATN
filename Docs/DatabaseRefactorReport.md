# Database refactor report

## Root cause of `Foreign key references invalid table 'Users'`

The application does **not** use the default ASP.NET Core Identity tables (`AspNetUsers`, `AspNetRoles`). It defines its own `User` and `Role` entities and maps them to `Users` and `Roles`. The error happens when a feature migration such as `AddPasswordResetOtp`, `AddSupportChat`, or `AddProductReviews` is applied to a database that does not contain the base `Users` table. SQL Server rejects the foreign key because the referenced table is absent.

## Connection string policy

- Primary connection: `ConnectionStrings:SqlExpressConnection` -> `Server=.\SQLEXPRESS;Database=DATN_PCStore;Trusted_Connection=True;TrustServerCertificate=True;`
- Fallback connection: `ConnectionStrings:LocalDbConnection` -> `Server=(localdb)\MSSQLLocalDB;Database=DATN_PCStore;Trusted_Connection=True;TrustServerCertificate=True;`
- `DatabaseConfiguration` tests the primary connection first and uses LocalDB only when SQL Express is unavailable.

## DbContext DbSet and table mapping

All DbSet mappings are explicit in `ApplicationDbContext.OnModelCreating`:

| DbSet | Entity | Table |
| --- | --- | --- |
| Roles | Role | Roles |
| Users | User | Users |
| Categories | Category | Categories |
| Products | Product | Products |
| ProductImages | ProductImage | ProductImages |
| Banners | Banner | Banners |
| Carts | Cart | Carts |
| CartItems | CartItem | CartItems |
| Orders | Order | Orders |
| OrderDetails | OrderDetail | OrderDetails |
| Warranties | Warranty | Warranties |
| WarrantyRequests | WarrantyRequest | WarrantyRequests |
| BuildPcConfigs | BuildPcConfig | BuildPcConfigs |
| BuildPcItems | BuildPcItem | BuildPcItems |
| Articles | Article | Articles |
| Feedbacks | Feedback | Feedbacks |
| SiteSettings | SiteSetting | SiteSettings |
| ShippingConfigs | ShippingConfig | ShippingConfigs |
| ShopLocations | ShopLocation | ShopLocations |
| PasswordResetOtps | PasswordResetOtp | PasswordResetOtps |
| ChatConversations | ChatConversation | ChatConversations |
| ChatMessages | ChatMessage | ChatMessages |
| ProductReviews | ProductReview | ProductReviews |

## Migrations checked for foreign keys to `Users`

- `202606030001_AddPasswordResetOtp`: creates `FK_PasswordResetOtps_Users_UserId`.
- `202606080001_AddSupportChat`: creates `FK_ChatConversations_Users_UserId`.
- `202606100001_AddWarrantyModule`: creates `FK_WarrantyRequests_Users_UserId` and joins `Users` during backfill.
- `202606140001_ImproveSupportChat`: joins `Users` during data backfill.
- `202606150001_AddProductReviews`: creates `FK_ProductReviews_Users_UserId`.

## Foreign key changes

The project remains mapped to the custom `Users` table, because the source code uses a custom user model and does not inherit from `IdentityDbContext`. No foreign key was changed to `AspNetUsers`. Defensive migration pre-checks were added before migrations that create new `Users` foreign keys so a damaged or incomplete database fails with a clear message instead of attempting to create a foreign key against a missing table.

## Safe startup migration

Application startup resolves the database connection, logs the selected connection name/server/database, lists pending EF Core migrations, runs `Database.MigrateAsync()`, and logs detailed errors if migration fails.

## Deploying to another Windows machine

1. Install SQL Server Express. If unavailable, install SQL Server LocalDB.
2. Copy the source code.
3. Restore NuGet packages and build the project.
4. Restore an existing `DATN_PCStore` database backup **or** run EF Core migrations against a database that already includes the base schema.
5. Run the application. It will try `.\SQLEXPRESS` first and then `(localdb)\MSSQLLocalDB` automatically.
6. Run `Scripts/database-health-check.sql` in SSMS to verify tables, foreign keys, indexes, and the expected `Users` table.
