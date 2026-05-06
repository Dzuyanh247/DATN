using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations;

public partial class AddOrderAddressFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF COL_LENGTH('Orders', 'AddressDetail') IS NULL ALTER TABLE Orders ADD AddressDetail nvarchar(250) NULL");
        migrationBuilder.Sql("IF COL_LENGTH('Orders', 'FullAddress') IS NULL ALTER TABLE Orders ADD FullAddress nvarchar(250) NULL");
        migrationBuilder.Sql("IF COL_LENGTH('Orders', 'ProvinceCode') IS NULL ALTER TABLE Orders ADD ProvinceCode nvarchar(20) NULL");
        migrationBuilder.Sql("IF COL_LENGTH('Orders', 'ProvinceName') IS NULL ALTER TABLE Orders ADD ProvinceName nvarchar(100) NULL");
        migrationBuilder.Sql("IF COL_LENGTH('Orders', 'WardCode') IS NULL ALTER TABLE Orders ADD WardCode nvarchar(20) NULL");
        migrationBuilder.Sql("IF COL_LENGTH('Orders', 'WardName') IS NULL ALTER TABLE Orders ADD WardName nvarchar(100) NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF COL_LENGTH('Orders', 'AddressDetail') IS NOT NULL ALTER TABLE Orders DROP COLUMN AddressDetail");
        migrationBuilder.Sql("IF COL_LENGTH('Orders', 'FullAddress') IS NOT NULL ALTER TABLE Orders DROP COLUMN FullAddress");
        migrationBuilder.Sql("IF COL_LENGTH('Orders', 'ProvinceCode') IS NOT NULL ALTER TABLE Orders DROP COLUMN ProvinceCode");
        migrationBuilder.Sql("IF COL_LENGTH('Orders', 'ProvinceName') IS NOT NULL ALTER TABLE Orders DROP COLUMN ProvinceName");
        migrationBuilder.Sql("IF COL_LENGTH('Orders', 'WardCode') IS NOT NULL ALTER TABLE Orders DROP COLUMN WardCode");
        migrationBuilder.Sql("IF COL_LENGTH('Orders', 'WardName') IS NOT NULL ALTER TABLE Orders DROP COLUMN WardName");
    }
}
