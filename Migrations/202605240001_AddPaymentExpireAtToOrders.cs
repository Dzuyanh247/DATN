using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations;

public partial class AddPaymentExpireAtToOrders : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF COL_LENGTH('Orders', 'PaymentExpireAt') IS NULL
BEGIN
    ALTER TABLE Orders ADD PaymentExpireAt datetime2 NULL;
END
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF COL_LENGTH('Orders', 'PaymentExpireAt') IS NOT NULL
BEGIN
    ALTER TABLE Orders DROP COLUMN PaymentExpireAt;
END
");
    }
}
