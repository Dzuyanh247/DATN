using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations;

public partial class ProductGalleryAndAdminUsers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'Username') IS NULL ALTER TABLE [Users] ADD [Username] nvarchar(60) NOT NULL DEFAULT('');
IF COL_LENGTH('Users', 'IsActive') IS NULL ALTER TABLE [Users] ADD [IsActive] bit NOT NULL DEFAULT(1);
UPDATE [Users] SET [Username] = LEFT([Email], CHARINDEX('@', [Email] + '@') - 1) WHERE [Username] = '';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Username') CREATE UNIQUE INDEX [IX_Users_Username] ON [Users]([Username]);

IF COL_LENGTH('Products', 'Slug') IS NULL ALTER TABLE [Products] ADD [Slug] nvarchar(220) NOT NULL DEFAULT('');
IF COL_LENGTH('Products', 'DiscountPrice') IS NULL ALTER TABLE [Products] ADD [DiscountPrice] decimal(18,2) NULL;
IF COL_LENGTH('Products', 'WarrantyMonths') IS NULL ALTER TABLE [Products] ADD [WarrantyMonths] int NOT NULL DEFAULT(12);
IF COL_LENGTH('Products', 'IsActive') IS NULL ALTER TABLE [Products] ADD [IsActive] bit NOT NULL DEFAULT(1);
IF COL_LENGTH('Products', 'ThumbnailImage') IS NULL ALTER TABLE [Products] ADD [ThumbnailImage] nvarchar(250) NOT NULL DEFAULT('');
IF COL_LENGTH('Products', 'Description') IS NULL ALTER TABLE [Products] ADD [Description] nvarchar(max) NOT NULL DEFAULT('');
UPDATE [Products] SET [Description] = ISNULL([DetailDescription], '') WHERE [Description] = '';
IF COL_LENGTH('Products', 'ThumbnailUrl') IS NOT NULL UPDATE [Products] SET [ThumbnailImage] = [ThumbnailUrl] WHERE [ThumbnailImage] = '';
UPDATE [Products] SET [DiscountPrice] = [SalePrice] WHERE [DiscountPrice] IS NULL AND [SalePrice] IS NOT NULL;
UPDATE [Products] SET [Slug] = CONCAT('product-', [Id]) WHERE [Slug] = '';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_Slug') CREATE UNIQUE INDEX [IX_Products_Slug] ON [Products]([Slug]);

IF COL_LENGTH('ProductImages', 'SortOrder') IS NULL ALTER TABLE [ProductImages] ADD [SortOrder] int NOT NULL DEFAULT(1);
IF COL_LENGTH('ProductImages', 'IsPrimary') IS NULL ALTER TABLE [ProductImages] ADD [IsPrimary] bit NOT NULL DEFAULT(0);

IF NOT EXISTS (SELECT 1 FROM [ProductImages])
BEGIN
    INSERT INTO [ProductImages]([ProductId], [ImageUrl], [SortOrder], [IsPrimary], [CreatedAt])
    SELECT [Id], [ThumbnailImage], 1, 1, GETUTCDATE() FROM [Products] WHERE [ThumbnailImage] <> '';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductImages_ProductId_SortOrder')
    CREATE INDEX [IX_ProductImages_ProductId_SortOrder] ON [ProductImages]([ProductId], [SortOrder]);
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductImages_ProductId_SortOrder') DROP INDEX [IX_ProductImages_ProductId_SortOrder] ON [ProductImages];
IF COL_LENGTH('ProductImages', 'IsPrimary') IS NOT NULL ALTER TABLE [ProductImages] DROP COLUMN [IsPrimary];
IF COL_LENGTH('ProductImages', 'SortOrder') IS NOT NULL ALTER TABLE [ProductImages] DROP COLUMN [SortOrder];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_Slug') DROP INDEX [IX_Products_Slug] ON [Products];
IF COL_LENGTH('Products', 'Description') IS NOT NULL ALTER TABLE [Products] DROP COLUMN [Description];
IF COL_LENGTH('Products', 'ThumbnailImage') IS NOT NULL ALTER TABLE [Products] DROP COLUMN [ThumbnailImage];
IF COL_LENGTH('Products', 'IsActive') IS NOT NULL ALTER TABLE [Products] DROP COLUMN [IsActive];
IF COL_LENGTH('Products', 'WarrantyMonths') IS NOT NULL ALTER TABLE [Products] DROP COLUMN [WarrantyMonths];
IF COL_LENGTH('Products', 'DiscountPrice') IS NOT NULL ALTER TABLE [Products] DROP COLUMN [DiscountPrice];
IF COL_LENGTH('Products', 'Slug') IS NOT NULL ALTER TABLE [Products] DROP COLUMN [Slug];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Username') DROP INDEX [IX_Users_Username] ON [Users];
IF COL_LENGTH('Users', 'IsActive') IS NOT NULL ALTER TABLE [Users] DROP COLUMN [IsActive];
IF COL_LENGTH('Users', 'Username') IS NOT NULL ALTER TABLE [Users] DROP COLUMN [Username];
");
    }
}
