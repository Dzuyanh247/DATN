using Datn.PcStore.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("202606150002_AddReviewHandler")]
public partial class AddReviewHandler : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>("HandledAt", "ProductReviews", nullable: true);
        migrationBuilder.AddColumn<int>("HandledByStaffId", "ProductReviews", nullable: true);
        migrationBuilder.AddColumn<string>("HandledByStaffName", "ProductReviews", type: "nvarchar(100)", maxLength: 100, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("HandledAt", "ProductReviews");
        migrationBuilder.DropColumn("HandledByStaffId", "ProductReviews");
        migrationBuilder.DropColumn("HandledByStaffName", "ProductReviews");
    }
}
