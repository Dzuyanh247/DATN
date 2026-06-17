using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations;

public partial class AddPasswordResetOtps : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF OBJECT_ID('Users', 'U') IS NULL
BEGIN
    THROW 51000, 'Migration requires table Users before creating PasswordResetOtps.', 1;
END

IF OBJECT_ID('PasswordResetOtps', 'U') IS NULL
BEGIN
    CREATE TABLE PasswordResetOtps (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PasswordResetOtps PRIMARY KEY,
        UserId INT NOT NULL,
        Email NVARCHAR(120) NOT NULL,
        CodeHash NVARCHAR(128) NOT NULL,
        ExpiresAt DATETIME2 NOT NULL,
        IsUsed BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UsedAt DATETIME2 NULL,
        CONSTRAINT FK_PasswordResetOtps_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PasswordResetOtps_UserId_IsUsed_ExpiresAt' AND object_id = OBJECT_ID('PasswordResetOtps'))
BEGIN
    CREATE INDEX IX_PasswordResetOtps_UserId_IsUsed_ExpiresAt ON PasswordResetOtps(UserId, IsUsed, ExpiresAt);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PasswordResetOtps_Email_CodeHash' AND object_id = OBJECT_ID('PasswordResetOtps'))
BEGIN
    CREATE INDEX IX_PasswordResetOtps_Email_CodeHash ON PasswordResetOtps(Email, CodeHash);
END
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF OBJECT_ID('PasswordResetOtps', 'U') IS NOT NULL
BEGIN
    DROP TABLE PasswordResetOtps;
END
");
    }
}
