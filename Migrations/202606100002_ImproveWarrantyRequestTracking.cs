using Datn.PcStore.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("202606100002_ImproveWarrantyRequestTracking")]
public partial class ImproveWarrantyRequestTracking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF COL_LENGTH('WarrantyRequests', 'RequestCode') IS NULL
    ALTER TABLE WarrantyRequests ADD RequestCode NVARCHAR(40) NULL;
");

        migrationBuilder.Sql(@"
UPDATE WarrantyRequests
SET RequestCode = CONCAT('YCBH', FORMAT(Id, '000000'))
WHERE RequestCode IS NULL OR LTRIM(RTRIM(RequestCode)) = '';

UPDATE WarrantyRequests
SET WarrantyCode = CONCAT('BH-DH', FORMAT(OrderId, '000000'), '-CT', FORMAT(OrderDetailId, '000000'))
WHERE OrderId IS NOT NULL AND OrderDetailId IS NOT NULL
  AND WarrantyCode <> CONCAT('BH-DH', FORMAT(OrderId, '000000'), '-CT', FORMAT(OrderDetailId, '000000'));

ALTER TABLE WarrantyRequests ALTER COLUMN RequestCode NVARCHAR(40) NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WarrantyRequests_RequestCode' AND object_id = OBJECT_ID('WarrantyRequests'))
    CREATE UNIQUE INDEX IX_WarrantyRequests_RequestCode ON WarrantyRequests(RequestCode);
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WarrantyRequests_RequestCode' AND object_id = OBJECT_ID('WarrantyRequests'))
    DROP INDEX IX_WarrantyRequests_RequestCode ON WarrantyRequests;
IF COL_LENGTH('WarrantyRequests', 'RequestCode') IS NOT NULL
    ALTER TABLE WarrantyRequests DROP COLUMN RequestCode;
");
    }
}
