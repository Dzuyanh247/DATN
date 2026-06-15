using Datn.PcStore.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("202606150005_AddChatMessageMetadata")]
public partial class AddChatMessageMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "MetadataJson",
            table: "ChatMessages",
            type: "nvarchar(max)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MetadataJson",
            table: "ChatMessages");
    }
}
