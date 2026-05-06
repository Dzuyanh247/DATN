using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations
{
    public partial class EnsurePromoBackgroundColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.SiteSettings', 'DealSectionBackgroundUrl') IS NULL
BEGIN
    ALTER TABLE [dbo].[SiteSettings]
    ADD [DealSectionBackgroundUrl] NVARCHAR(1000) NULL;
END");

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.SiteSettings', 'HotPromotionBackgroundUrl') IS NULL
BEGIN
    ALTER TABLE [dbo].[SiteSettings]
    ADD [HotPromotionBackgroundUrl] NVARCHAR(1000) NULL;
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.SiteSettings', 'DealSectionBackgroundUrl') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[SiteSettings]
    DROP COLUMN [DealSectionBackgroundUrl];
END");

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.SiteSettings', 'HotPromotionBackgroundUrl') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[SiteSettings]
    DROP COLUMN [HotPromotionBackgroundUrl];
END");
        }
    }
}
