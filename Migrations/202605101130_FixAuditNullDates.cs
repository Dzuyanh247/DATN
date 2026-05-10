using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations;

public partial class FixAuditNullDates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var tables = new[]
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

        foreach (var table in tables)
        {
            migrationBuilder.Sql($@"
IF OBJECT_ID('{table}', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('{table}', 'CreatedAt') IS NOT NULL
    BEGIN
        UPDATE [{table}] SET [CreatedAt] = GETUTCDATE() WHERE [CreatedAt] IS NULL;

        IF EXISTS (
            SELECT 1
            FROM sys.columns c
            JOIN sys.tables t ON t.object_id = c.object_id
            JOIN sys.types ty ON ty.user_type_id = c.user_type_id
            WHERE t.name = '{table}'
              AND c.name = 'CreatedAt'
              AND c.is_nullable = 1
              AND ty.name = 'datetime2'
        )
        BEGIN
            ALTER TABLE [{table}] ALTER COLUMN [CreatedAt] DATETIME2 NOT NULL;
        END

        IF NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            JOIN sys.columns c ON c.default_object_id = dc.object_id
            JOIN sys.tables t ON t.object_id = c.object_id
            WHERE t.name = '{table}' AND c.name = 'CreatedAt'
        )
        BEGIN
            ALTER TABLE [{table}] ADD CONSTRAINT [DF_{table}_CreatedAt] DEFAULT GETUTCDATE() FOR [CreatedAt];
        END
    END

    IF COL_LENGTH('{table}', 'UpdatedAt') IS NOT NULL
    BEGIN
        UPDATE [{table}] SET [UpdatedAt] = GETUTCDATE() WHERE [UpdatedAt] IS NULL;

        IF EXISTS (
            SELECT 1
            FROM sys.columns c
            JOIN sys.tables t ON t.object_id = c.object_id
            JOIN sys.types ty ON ty.user_type_id = c.user_type_id
            WHERE t.name = '{table}'
              AND c.name = 'UpdatedAt'
              AND c.is_nullable = 1
              AND ty.name = 'datetime2'
        )
        BEGIN
            ALTER TABLE [{table}] ALTER COLUMN [UpdatedAt] DATETIME2 NOT NULL;
        END

        IF NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            JOIN sys.columns c ON c.default_object_id = dc.object_id
            JOIN sys.tables t ON t.object_id = c.object_id
            WHERE t.name = '{table}' AND c.name = 'UpdatedAt'
        )
        BEGIN
            ALTER TABLE [{table}] ADD CONSTRAINT [DF_{table}_UpdatedAt] DEFAULT GETUTCDATE() FOR [UpdatedAt];
        END
    END
END");
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
