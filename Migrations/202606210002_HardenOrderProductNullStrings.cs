using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations;

public partial class HardenOrderProductNullStrings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
UPDATE Orders SET ReceiverName = N'' WHERE ReceiverName IS NULL;
UPDATE Orders SET ReceiverPhone = N'' WHERE ReceiverPhone IS NULL;
UPDATE Orders SET ShippingAddress = N'' WHERE ShippingAddress IS NULL;
UPDATE Orders SET PaymentMethod = N'COD' WHERE PaymentMethod IS NULL OR LTRIM(RTRIM(PaymentMethod)) = N'';
UPDATE Orders SET PaymentStatus = N'UNPAID' WHERE PaymentStatus IS NULL OR LTRIM(RTRIM(PaymentStatus)) = N'';
UPDATE Orders SET CheckoutMode = N'cart' WHERE CheckoutMode IS NULL OR LTRIM(RTRIM(CheckoutMode)) = N'';
UPDATE OrderDetails SET ProductName = N'Sản phẩm không xác định' WHERE ProductName IS NULL OR LTRIM(RTRIM(ProductName)) = N'';
UPDATE OrderDetails SET ProductImage = N'/images/placeholders/product.svg' WHERE ProductImage IS NULL OR LTRIM(RTRIM(ProductImage)) = N'';
UPDATE Products SET Name = N'Sản phẩm không xác định' WHERE Name IS NULL OR LTRIM(RTRIM(Name)) = N'';
UPDATE Products SET Slug = CONCAT(N'product-', Id) WHERE Slug IS NULL OR LTRIM(RTRIM(Slug)) = N'';
UPDATE Products SET ProductCode = CONCAT(N'P-', Id) WHERE ProductCode IS NULL OR LTRIM(RTRIM(ProductCode)) = N'';
UPDATE Products SET SourceUrl = N'' WHERE SourceUrl IS NULL;
UPDATE Products SET ThumbnailImage = N'' WHERE ThumbnailImage IS NULL;
UPDATE Products SET WarrantyDuration = CONCAT(WarrantyMonths, N' tháng') WHERE WarrantyDuration IS NULL OR LTRIM(RTRIM(WarrantyDuration)) = N'';
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
