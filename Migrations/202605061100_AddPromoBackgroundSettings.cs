using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations
{
    public partial class AddPromoBackgroundSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DealSectionBackgroundUrl",
                table: "SiteSettings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HotPromotionBackgroundUrl",
                table: "SiteSettings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DealSectionBackgroundUrl",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HotPromotionBackgroundUrl",
                table: "SiteSettings");
        }
    }
}
