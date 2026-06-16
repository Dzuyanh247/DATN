using Datn.PcStore.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("202606080001_AddSupportChat")]
public partial class AddSupportChat : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF OBJECT_ID('Users', 'U') IS NULL
BEGIN
    THROW 51000, 'Migration requires table Users. This project uses custom Users/Roles tables, not ASP.NET Identity AspNetUsers. Restore the base schema or run the initial schema script before applying feature migrations.', 1;
END

IF OBJECT_ID('ChatConversations', 'U') IS NULL
BEGIN
    CREATE TABLE ChatConversations (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChatConversations PRIMARY KEY,
        UserId INT NULL,
        GuestName NVARCHAR(100) NULL,
        GuestEmail NVARCHAR(120) NULL,
        GuestPhone NVARCHAR(20) NULL,
        AccessToken NVARCHAR(64) NOT NULL,
        Status INT NOT NULL CONSTRAINT DF_ChatConversations_Status DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ChatConversations_CreatedAt DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_ChatConversations_UpdatedAt DEFAULT GETUTCDATE(),
        CONSTRAINT FK_ChatConversations_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
    );
END

IF OBJECT_ID('ChatMessages', 'U') IS NULL
BEGIN
    CREATE TABLE ChatMessages (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChatMessages PRIMARY KEY,
        ConversationId INT NOT NULL,
        SenderType INT NOT NULL,
        Message NVARCHAR(1000) NOT NULL,
        IsRead BIT NOT NULL CONSTRAINT DF_ChatMessages_IsRead DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ChatMessages_CreatedAt DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_ChatMessages_UpdatedAt DEFAULT GETUTCDATE(),
        CONSTRAINT FK_ChatMessages_ChatConversations_ConversationId FOREIGN KEY (ConversationId) REFERENCES ChatConversations(Id) ON DELETE CASCADE
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChatConversations_AccessToken' AND object_id = OBJECT_ID('ChatConversations'))
    CREATE UNIQUE INDEX IX_ChatConversations_AccessToken ON ChatConversations(AccessToken);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChatConversations_Status_UpdatedAt' AND object_id = OBJECT_ID('ChatConversations'))
    CREATE INDEX IX_ChatConversations_Status_UpdatedAt ON ChatConversations(Status, UpdatedAt);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChatConversations_UserId' AND object_id = OBJECT_ID('ChatConversations'))
    CREATE INDEX IX_ChatConversations_UserId ON ChatConversations(UserId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChatMessages_ConversationId_CreatedAt' AND object_id = OBJECT_ID('ChatMessages'))
    CREATE INDEX IX_ChatMessages_ConversationId_CreatedAt ON ChatMessages(ConversationId, CreatedAt);
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF OBJECT_ID('ChatMessages', 'U') IS NOT NULL DROP TABLE ChatMessages;
IF OBJECT_ID('ChatConversations', 'U') IS NOT NULL DROP TABLE ChatConversations;
");
    }
}
