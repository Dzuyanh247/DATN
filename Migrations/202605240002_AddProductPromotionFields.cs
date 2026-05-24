using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations;

public partial class AddProductPromotionFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsHotSale",
            table: "Products",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "IsDailyDeal",
            table: "Products",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "IsPromotion",
            table: "Products",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "PromotionStartDate",
            table: "Products",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "PromotionEndDate",
            table: "Products",
            type: "datetime2",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PromotionEndDate",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "PromotionStartDate",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "IsPromotion",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "IsDailyDeal",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "IsHotSale",
            table: "Products");
    }
}
