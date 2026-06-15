using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Datn.PcStore.Data;

#nullable disable

namespace Datn.PcStore.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("202606150004_AddSupportChatAutomation")]
public partial class AddSupportChatAutomation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF COL_LENGTH('ChatConversations', 'Topic') IS NULL ALTER TABLE ChatConversations ADD Topic NVARCHAR(50) NULL;");
        migrationBuilder.Sql("IF COL_LENGTH('ChatConversations', 'NeedsStaff') IS NULL ALTER TABLE ChatConversations ADD NeedsStaff BIT NOT NULL CONSTRAINT DF_ChatConversations_NeedsStaff DEFAULT 0;");
        migrationBuilder.Sql("IF COL_LENGTH('ChatConversations', 'Priority') IS NULL ALTER TABLE ChatConversations ADD Priority INT NOT NULL CONSTRAINT DF_ChatConversations_Priority DEFAULT 0;");
        migrationBuilder.Sql("IF COL_LENGTH('ChatConversations', 'AutomationContext') IS NULL ALTER TABLE ChatConversations ADD AutomationContext NVARCHAR(2000) NULL;");
        migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChatConversations_NeedsStaff_Priority' AND object_id = OBJECT_ID('ChatConversations')) CREATE INDEX IX_ChatConversations_NeedsStaff_Priority ON ChatConversations(NeedsStaff, Priority);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChatConversations_NeedsStaff_Priority' AND object_id = OBJECT_ID('ChatConversations')) DROP INDEX IX_ChatConversations_NeedsStaff_Priority ON ChatConversations;");
        migrationBuilder.Sql("IF COL_LENGTH('ChatConversations', 'AutomationContext') IS NOT NULL ALTER TABLE ChatConversations DROP COLUMN AutomationContext;");
        migrationBuilder.Sql(@"DECLARE @priorityDefault sysname;
SELECT @priorityDefault = dc.name FROM sys.default_constraints dc
JOIN sys.columns c ON c.default_object_id = dc.object_id
WHERE dc.parent_object_id = OBJECT_ID('ChatConversations') AND c.name = 'Priority';
IF @priorityDefault IS NOT NULL EXEC('ALTER TABLE ChatConversations DROP CONSTRAINT [' + @priorityDefault + ']');
IF COL_LENGTH('ChatConversations', 'Priority') IS NOT NULL ALTER TABLE ChatConversations DROP COLUMN Priority;");
        migrationBuilder.Sql(@"DECLARE @needsStaffDefault sysname;
SELECT @needsStaffDefault = dc.name FROM sys.default_constraints dc
JOIN sys.columns c ON c.default_object_id = dc.object_id
WHERE dc.parent_object_id = OBJECT_ID('ChatConversations') AND c.name = 'NeedsStaff';
IF @needsStaffDefault IS NOT NULL EXEC('ALTER TABLE ChatConversations DROP CONSTRAINT [' + @needsStaffDefault + ']');
IF COL_LENGTH('ChatConversations', 'NeedsStaff') IS NOT NULL ALTER TABLE ChatConversations DROP COLUMN NeedsStaff;");
        migrationBuilder.Sql("IF COL_LENGTH('ChatConversations', 'Topic') IS NOT NULL ALTER TABLE ChatConversations DROP COLUMN Topic;");
    }
}
