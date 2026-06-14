using Datn.PcStore.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("202606140001_ImproveSupportChat")]
public partial class ImproveSupportChat : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF COL_LENGTH('ChatConversations', 'CustomerId') IS NULL ALTER TABLE ChatConversations ADD CustomerId INT NULL;
IF COL_LENGTH('ChatConversations', 'GuestId') IS NULL ALTER TABLE ChatConversations ADD GuestId NVARCHAR(64) NULL;
IF COL_LENGTH('ChatConversations', 'CustomerName') IS NULL ALTER TABLE ChatConversations ADD CustomerName NVARCHAR(100) NULL;
IF COL_LENGTH('ChatConversations', 'CustomerEmail') IS NULL ALTER TABLE ChatConversations ADD CustomerEmail NVARCHAR(120) NULL;
IF COL_LENGTH('ChatConversations', 'CustomerPhone') IS NULL ALTER TABLE ChatConversations ADD CustomerPhone NVARCHAR(20) NULL;
IF COL_LENGTH('ChatConversations', 'AssignedStaffId') IS NULL ALTER TABLE ChatConversations ADD AssignedStaffId INT NULL;
IF COL_LENGTH('ChatConversations', 'AssignedStaffName') IS NULL ALTER TABLE ChatConversations ADD AssignedStaffName NVARCHAR(100) NULL;
IF COL_LENGTH('ChatConversations', 'ClosedAt') IS NULL ALTER TABLE ChatConversations ADD ClosedAt DATETIME2 NULL;
IF COL_LENGTH('ChatConversations', 'LastMessageAt') IS NULL ALTER TABLE ChatConversations ADD LastMessageAt DATETIME2 NULL;
IF COL_LENGTH('ChatConversations', 'StaffUnreadCount') IS NULL ALTER TABLE ChatConversations ADD StaffUnreadCount INT NOT NULL CONSTRAINT DF_ChatConversations_StaffUnreadCount DEFAULT 0;

IF COL_LENGTH('ChatMessages', 'SenderUserId') IS NULL ALTER TABLE ChatMessages ADD SenderUserId INT NULL;
IF COL_LENGTH('ChatMessages', 'SenderName') IS NULL ALTER TABLE ChatMessages ADD SenderName NVARCHAR(100) NULL;
IF COL_LENGTH('ChatMessages', 'IsSystem') IS NULL ALTER TABLE ChatMessages ADD IsSystem BIT NOT NULL CONSTRAINT DF_ChatMessages_IsSystem DEFAULT 0;
IF COL_LENGTH('ChatMessages', 'ReadAt') IS NULL ALTER TABLE ChatMessages ADD ReadAt DATETIME2 NULL;

UPDATE ChatConversations SET
    CustomerId = COALESCE(CustomerId, UserId),
    CustomerName = COALESCE(NULLIF(CustomerName, ''), GuestName),
    CustomerEmail = COALESCE(NULLIF(CustomerEmail, ''), GuestEmail),
    CustomerPhone = COALESCE(NULLIF(CustomerPhone, ''), GuestPhone),
    LastMessageAt = COALESCE(LastMessageAt, UpdatedAt, CreatedAt);

UPDATE c SET
    CustomerName = COALESCE(NULLIF(c.CustomerName, ''), u.FullName),
    CustomerEmail = COALESCE(NULLIF(c.CustomerEmail, ''), u.Email),
    CustomerPhone = COALESCE(NULLIF(c.CustomerPhone, ''), u.Phone)
FROM ChatConversations c LEFT JOIN Users u ON u.Id = COALESCE(c.CustomerId, c.UserId);

UPDATE ChatMessages SET
    IsSystem = CASE WHEN SenderType = 3 THEN 1 ELSE IsSystem END,
    SenderName = CASE SenderType WHEN 1 THEN COALESCE(SenderName, N'Khách hàng') WHEN 2 THEN COALESCE(SenderName, N'Nhân viên hỗ trợ') ELSE COALESCE(SenderName, N'Hệ thống') END,
    ReadAt = CASE WHEN IsRead = 1 THEN COALESCE(ReadAt, UpdatedAt, CreatedAt) ELSE ReadAt END;

UPDATE c SET StaffUnreadCount = unread.Total
FROM ChatConversations c
CROSS APPLY (SELECT COUNT(*) Total FROM ChatMessages m WHERE m.ConversationId = c.Id AND m.SenderType = 1 AND m.IsRead = 0) unread;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChatConversations_CustomerId_Status' AND object_id = OBJECT_ID('ChatConversations'))
    CREATE INDEX IX_ChatConversations_CustomerId_Status ON ChatConversations(CustomerId, Status);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChatConversations_GuestId_Status' AND object_id = OBJECT_ID('ChatConversations'))
    CREATE INDEX IX_ChatConversations_GuestId_Status ON ChatConversations(GuestId, Status);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChatConversations_Status_LastMessageAt' AND object_id = OBJECT_ID('ChatConversations'))
    CREATE INDEX IX_ChatConversations_Status_LastMessageAt ON ChatConversations(Status, LastMessageAt);
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Deliberately non-destructive: chat history and metadata must never be dropped.
    }
}
