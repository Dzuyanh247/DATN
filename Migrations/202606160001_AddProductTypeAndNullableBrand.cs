using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations
{
    public partial class AddProductTypeAndNullableBrand : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Brand",
                table: "Products",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AddColumn<string>(
                name: "ProductType",
                table: "Products",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PC");

            migrationBuilder.Sql(@"
UPDATE Products
SET ProductType = CASE
    WHEN ComponentType IN ('CPU','Mainboard','RAM','VGA','SSD','HDD','SSD/HDD','PSU','Case','Cooler','Monitor','Keyboard','Mouse','Headphone','MonitorArm','Component') THEN 'Component'
    ELSE 'PC'
END;
UPDATE Products SET ComponentType = 'SSD' WHERE ComponentType = 'SSD/HDD';
UPDATE Products SET Brand = NULL WHERE Brand = '' OR Brand = 'N/A';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ProductType", table: "Products");
            migrationBuilder.Sql("UPDATE Products SET Brand = 'N/A' WHERE Brand IS NULL");
            migrationBuilder.AlterColumn<string>(
                name: "Brand",
                table: "Products",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(80)",
                oldMaxLength: 80,
                oldNullable: true);
        }
    }
}
