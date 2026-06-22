using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations;

public partial class AllowNullableProductSourceUrl : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF COL_LENGTH('Products', 'SourceUrl') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_SourceUrl' AND object_id = OBJECT_ID(N'Products'))
        DROP INDEX IX_Products_SourceUrl ON Products;

    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'Products')
          AND name = N'SourceUrl'
          AND is_nullable = 0
    )
        ALTER TABLE Products ALTER COLUMN SourceUrl nvarchar(1000) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_SourceUrl' AND object_id = OBJECT_ID(N'Products'))
        CREATE INDEX IX_Products_SourceUrl ON Products(SourceUrl);
END
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF COL_LENGTH('Products', 'SourceUrl') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_SourceUrl' AND object_id = OBJECT_ID(N'Products'))
        DROP INDEX IX_Products_SourceUrl ON Products;

    UPDATE Products SET SourceUrl = N'' WHERE SourceUrl IS NULL;
    ALTER TABLE Products ALTER COLUMN SourceUrl nvarchar(1000) NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_SourceUrl' AND object_id = OBJECT_ID(N'Products'))
        CREATE INDEX IX_Products_SourceUrl ON Products(SourceUrl);
END
");
    }
}
