using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations;

public partial class AddPositionColumnToBanners : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF COL_LENGTH('Banners', 'Position') IS NULL
BEGIN
    ALTER TABLE [Banners] ADD [Position] NVARCHAR(50) NOT NULL CONSTRAINT [DF_Banners_Position] DEFAULT 'MainBanner';
END

UPDATE [Banners]
SET [Position] = 'MainBanner'
WHERE [Position] IS NULL OR [Position] = '';
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF COL_LENGTH('Banners', 'Position') IS NOT NULL
BEGIN
    DECLARE @ConstraintName NVARCHAR(200);
    SELECT @ConstraintName = d.name
    FROM sys.default_constraints d
    INNER JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
    WHERE d.parent_object_id = OBJECT_ID('Banners') AND c.name = 'Position';

    IF @ConstraintName IS NOT NULL
        EXEC('ALTER TABLE [Banners] DROP CONSTRAINT [' + @ConstraintName + ']');

    ALTER TABLE [Banners] DROP COLUMN [Position];
END
");
    }
}
