using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations;

public partial class EnsureSiteSettingsBackgroundColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF COL_LENGTH('SiteSettings', 'DealSectionBackgroundUrl') IS NULL
BEGIN
    ALTER TABLE SiteSettings ADD DealSectionBackgroundUrl NVARCHAR(1000) NULL;
END

IF COL_LENGTH('SiteSettings', 'HotPromotionBackgroundUrl') IS NULL
BEGIN
    ALTER TABLE SiteSettings ADD HotPromotionBackgroundUrl NVARCHAR(1000) NULL;
END
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF COL_LENGTH('SiteSettings', 'DealSectionBackgroundUrl') IS NOT NULL
BEGIN
    ALTER TABLE SiteSettings DROP COLUMN DealSectionBackgroundUrl;
END

IF COL_LENGTH('SiteSettings', 'HotPromotionBackgroundUrl') IS NOT NULL
BEGIN
    ALTER TABLE SiteSettings DROP COLUMN HotPromotionBackgroundUrl;
END
");
    }
}
