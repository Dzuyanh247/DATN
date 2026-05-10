using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations
{
    public partial class AddShippingFreeAndDefaultShopColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FreeShippingDistanceKm",
                table: "ShippingConfigs",
                type: "decimal(8,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ShippingConfigs",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "ShopLocations",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "FreeShippingDistanceKm", table: "ShippingConfigs");
            migrationBuilder.DropColumn(name: "IsActive", table: "ShippingConfigs");
            migrationBuilder.DropColumn(name: "IsDefault", table: "ShopLocations");
        }
    }
}
