using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations
{
    public partial class FixNullableProductTextFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE Products SET ProductType = 'PC' WHERE ProductType IS NULL OR LTRIM(RTRIM(ProductType)) = '';
UPDATE Products SET ComponentType = CASE WHEN ProductType = 'Component' THEN 'Other' ELSE 'PC' END WHERE ComponentType IS NULL OR LTRIM(RTRIM(ComponentType)) = '';
UPDATE Products SET ThumbnailImage = '' WHERE ThumbnailImage IS NULL;
UPDATE Products SET ShortDescription = '' WHERE ShortDescription IS NULL;
UPDATE Products SET Description = '' WHERE Description IS NULL;
UPDATE Products SET DetailDescription = '' WHERE DetailDescription IS NULL;
UPDATE Products SET Specifications = '' WHERE Specifications IS NULL;
UPDATE Products SET WarrantyDuration = CONCAT(WarrantyMonths, N' tháng') WHERE WarrantyDuration IS NULL OR LTRIM(RTRIM(WarrantyDuration)) = '';
");

            migrationBuilder.AlterColumn<string>(name: "ProductType", table: "Products", type: "nvarchar(20)", maxLength: 20, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(20)", oldMaxLength: 20);
            migrationBuilder.AlterColumn<string>(name: "ComponentType", table: "Products", type: "nvarchar(40)", maxLength: 40, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(40)", oldMaxLength: 40);
            migrationBuilder.AlterColumn<string>(name: "ThumbnailImage", table: "Products", type: "nvarchar(1000)", maxLength: 1000, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(1000)", oldMaxLength: 1000);
            migrationBuilder.AlterColumn<string>(name: "ShortDescription", table: "Products", type: "nvarchar(500)", maxLength: 500, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(500)", oldMaxLength: 500);
            migrationBuilder.AlterColumn<string>(name: "Description", table: "Products", type: "nvarchar(max)", nullable: true, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "DetailDescription", table: "Products", type: "nvarchar(max)", nullable: true, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Specifications", table: "Products", type: "nvarchar(max)", nullable: true, oldClrType: typeof(string), oldType: "nvarchar(max)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE Products SET ProductType = 'PC' WHERE ProductType IS NULL;
UPDATE Products SET ComponentType = 'Other' WHERE ComponentType IS NULL;
UPDATE Products SET ThumbnailImage = '' WHERE ThumbnailImage IS NULL;
UPDATE Products SET ShortDescription = '' WHERE ShortDescription IS NULL;
UPDATE Products SET Description = '' WHERE Description IS NULL;
UPDATE Products SET DetailDescription = '' WHERE DetailDescription IS NULL;
UPDATE Products SET Specifications = '' WHERE Specifications IS NULL;
");

            migrationBuilder.AlterColumn<string>(name: "ProductType", table: "Products", type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "PC", oldClrType: typeof(string), oldType: "nvarchar(20)", oldMaxLength: 20, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "ComponentType", table: "Products", type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "Other", oldClrType: typeof(string), oldType: "nvarchar(40)", oldMaxLength: 40, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "ThumbnailImage", table: "Products", type: "nvarchar(1000)", maxLength: 1000, nullable: false, defaultValue: "", oldClrType: typeof(string), oldType: "nvarchar(1000)", oldMaxLength: 1000, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "ShortDescription", table: "Products", type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: "", oldClrType: typeof(string), oldType: "nvarchar(500)", oldMaxLength: 500, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Description", table: "Products", type: "nvarchar(max)", nullable: false, defaultValue: "", oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "DetailDescription", table: "Products", type: "nvarchar(max)", nullable: false, defaultValue: "", oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Specifications", table: "Products", type: "nvarchar(max)", nullable: false, defaultValue: "", oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
        }
    }
}
