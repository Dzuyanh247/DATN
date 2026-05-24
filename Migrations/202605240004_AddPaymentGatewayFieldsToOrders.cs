using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations;

public partial class AddPaymentGatewayFieldsToOrders : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF COL_LENGTH('Orders', 'PaymentUrl') IS NULL
BEGIN
    ALTER TABLE Orders ADD PaymentUrl nvarchar(500) NULL;
END
IF COL_LENGTH('Orders', 'PaymentTransactionId') IS NULL
BEGIN
    ALTER TABLE Orders ADD PaymentTransactionId nvarchar(100) NULL;
END
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF COL_LENGTH('Orders', 'PaymentUrl') IS NOT NULL
BEGIN
    ALTER TABLE Orders DROP COLUMN PaymentUrl;
END
IF COL_LENGTH('Orders', 'PaymentTransactionId') IS NOT NULL
BEGIN
    ALTER TABLE Orders DROP COLUMN PaymentTransactionId;
END
");
    }
}
