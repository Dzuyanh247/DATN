using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations;

public partial class AddArticlePinning : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF COL_LENGTH('Articles', 'IsPinned') IS NULL ALTER TABLE Articles ADD IsPinned BIT NOT NULL CONSTRAINT DF_Articles_IsPinned DEFAULT 0;");
        migrationBuilder.Sql("IF COL_LENGTH('Articles', 'PinnedAt') IS NULL ALTER TABLE Articles ADD PinnedAt DATETIME2 NULL;");
        migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Articles_IsPublished_IsPinned_PinnedAt_CreatedAt' AND object_id = OBJECT_ID('Articles')) CREATE INDEX IX_Articles_IsPublished_IsPinned_PinnedAt_CreatedAt ON Articles(IsPublished, IsPinned, PinnedAt, CreatedAt);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Articles_IsPublished_IsPinned_PinnedAt_CreatedAt' AND object_id = OBJECT_ID('Articles')) DROP INDEX IX_Articles_IsPublished_IsPinned_PinnedAt_CreatedAt ON Articles;");
        migrationBuilder.Sql("DECLARE @pinnedDefault sysname; SELECT @pinnedDefault = dc.name FROM sys.default_constraints dc INNER JOIN sys.columns c ON c.default_object_id = dc.object_id WHERE dc.parent_object_id = OBJECT_ID('Articles') AND c.name = 'IsPinned'; IF @pinnedDefault IS NOT NULL EXEC('ALTER TABLE Articles DROP CONSTRAINT [' + @pinnedDefault + ']');");
        migrationBuilder.Sql("IF COL_LENGTH('Articles', 'PinnedAt') IS NOT NULL ALTER TABLE Articles DROP COLUMN PinnedAt;");
        migrationBuilder.Sql("IF COL_LENGTH('Articles', 'IsPinned') IS NOT NULL ALTER TABLE Articles DROP COLUMN IsPinned;");
    }
}
