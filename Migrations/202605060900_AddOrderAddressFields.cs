using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations;

public partial class AddOrderAddressFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AddressDetail",
            table: "Orders",
            type: "nvarchar(250)",
            maxLength: 250,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "FullAddress",
            table: "Orders",
            type: "nvarchar(250)",
            maxLength: 250,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ProvinceCode",
            table: "Orders",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ProvinceName",
            table: "Orders",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "WardCode",
            table: "Orders",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "WardName",
            table: "Orders",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AddressDetail", table: "Orders");
        migrationBuilder.DropColumn(name: "FullAddress", table: "Orders");
        migrationBuilder.DropColumn(name: "ProvinceCode", table: "Orders");
        migrationBuilder.DropColumn(name: "ProvinceName", table: "Orders");
        migrationBuilder.DropColumn(name: "WardCode", table: "Orders");
        migrationBuilder.DropColumn(name: "WardName", table: "Orders");
    }
}
