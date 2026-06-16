using System;
using Datn.PcStore.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace Datn.PcStore.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("202606150001_AddProductReviews")]
public partial class AddProductReviews : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF OBJECT_ID('Users', 'U') IS NULL
    THROW 51000, 'Migration requires table Users. This project uses custom Users/Roles tables, not ASP.NET Identity AspNetUsers. Restore the base schema or run the initial schema script before applying feature migrations.', 1;
IF OBJECT_ID('Products', 'U') IS NULL
    THROW 51000, 'Migration requires table Products before creating ProductReviews.', 1;
IF OBJECT_ID('Orders', 'U') IS NULL
    THROW 51000, 'Migration requires table Orders before creating ProductReviews.', 1;
IF OBJECT_ID('OrderDetails', 'U') IS NULL
    THROW 51000, 'Migration requires table OrderDetails before creating ProductReviews.', 1;
");

        migrationBuilder.CreateTable(name: "ProductReviews", columns: table => new
        {
            Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            ProductId = table.Column<int>(type: "int", nullable: false), UserId = table.Column<int>(type: "int", nullable: false),
            OrderId = table.Column<int>(type: "int", nullable: false), OrderDetailId = table.Column<int>(type: "int", nullable: false),
            Rating = table.Column<int>(type: "int", nullable: false), Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
            Status = table.Column<int>(type: "int", nullable: false, defaultValue: 2), AdminReply = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
            AdminRepliedAt = table.Column<DateTime>(type: "datetime2", nullable: true), HelpfulCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
            CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"), UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
        }, constraints: table =>
        {
            table.PrimaryKey("PK_ProductReviews", x => x.Id);
            table.ForeignKey("FK_ProductReviews_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_ProductReviews_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_ProductReviews_Orders_OrderId", x => x.OrderId, "Orders", "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_ProductReviews_OrderDetails_OrderDetailId", x => x.OrderDetailId, "OrderDetails", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_ProductReviews_ProductId", "ProductReviews", "ProductId");
        migrationBuilder.CreateIndex("IX_ProductReviews_UserId", "ProductReviews", "UserId"); migrationBuilder.CreateIndex("IX_ProductReviews_OrderId", "ProductReviews", "OrderId");
        migrationBuilder.CreateIndex("IX_ProductReviews_OrderDetailId", "ProductReviews", "OrderDetailId"); migrationBuilder.CreateIndex("IX_ProductReviews_CreatedAt", "ProductReviews", "CreatedAt");
        migrationBuilder.CreateIndex("IX_ProductReviews_Rating", "ProductReviews", "Rating");
        migrationBuilder.CreateIndex("IX_ProductReviews_ProductId_UserId_OrderId", "ProductReviews", new[] { "ProductId", "UserId", "OrderId" }, unique: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("ProductReviews");
}
