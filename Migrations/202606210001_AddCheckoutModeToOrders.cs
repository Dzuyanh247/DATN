using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations;

public partial class AddCheckoutModeToOrders : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF COL_LENGTH('Orders', 'CheckoutMode') IS NULL
BEGIN
    ALTER TABLE Orders ADD CheckoutMode nvarchar(30) NOT NULL CONSTRAINT DF_Orders_CheckoutMode DEFAULT 'cart';
END
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF COL_LENGTH('Orders', 'CheckoutMode') IS NOT NULL
BEGIN
    DECLARE @constraintName nvarchar(200);
    SELECT @constraintName = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    WHERE t.name = 'Orders' AND c.name = 'CheckoutMode';
    IF @constraintName IS NOT NULL EXEC('ALTER TABLE Orders DROP CONSTRAINT ' + @constraintName);
    ALTER TABLE Orders DROP COLUMN CheckoutMode;
END
");
    }
}
