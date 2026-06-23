using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations;

public partial class AddVoucherMaxOrderAmount : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF COL_LENGTH('Vouchers', 'MaxOrderAmount') IS NULL ALTER TABLE Vouchers ADD MaxOrderAmount DECIMAL(18,2) NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF COL_LENGTH('Vouchers', 'MaxOrderAmount') IS NOT NULL ALTER TABLE Vouchers DROP COLUMN MaxOrderAmount;");
    }
}
