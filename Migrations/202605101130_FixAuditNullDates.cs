using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations;

public partial class FixAuditNullDates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var tables = new[]
        {
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
        ALTER TABLE [{table}] ALTER COLUMN [CreatedAt] DATETIME2 NOT NULL;
    END

    IF COL_LENGTH('{table}', 'UpdatedAt') IS NOT NULL
    BEGIN
        UPDATE [{table}] SET [UpdatedAt] = GETUTCDATE() WHERE [UpdatedAt] IS NULL;
        ALTER TABLE [{table}] ALTER COLUMN [UpdatedAt] DATETIME2 NOT NULL;
    END
END");
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
