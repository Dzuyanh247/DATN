using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations
{
    public partial class RepairProductPromotionColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF COL_LENGTH('Products', 'IsHotSale') IS NULL
BEGIN
    ALTER TABLE Products ADD IsHotSale bit NOT NULL CONSTRAINT DF_Products_IsHotSale DEFAULT(0);
END

IF COL_LENGTH('Products', 'IsDailyDeal') IS NULL
BEGIN
    ALTER TABLE Products ADD IsDailyDeal bit NOT NULL CONSTRAINT DF_Products_IsDailyDeal DEFAULT(0);
END

IF COL_LENGTH('Products', 'IsPromotion') IS NULL
BEGIN
    ALTER TABLE Products ADD IsPromotion bit NOT NULL CONSTRAINT DF_Products_IsPromotion DEFAULT(0);
END

IF COL_LENGTH('Products', 'PromotionStartDate') IS NULL
BEGIN
    ALTER TABLE Products ADD PromotionStartDate datetime2 NULL;
END

IF COL_LENGTH('Products', 'PromotionEndDate') IS NULL
BEGIN
    ALTER TABLE Products ADD PromotionEndDate datetime2 NULL;
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
