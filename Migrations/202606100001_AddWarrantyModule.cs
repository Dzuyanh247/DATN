using Datn.PcStore.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("202606100001_AddWarrantyModule")]
public partial class AddWarrantyModule : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF COL_LENGTH('Products', 'WarrantyMonths') IS NULL
    ALTER TABLE Products ADD WarrantyMonths INT NOT NULL CONSTRAINT DF_Products_WarrantyMonths DEFAULT 12;
IF COL_LENGTH('OrderDetails', 'WarrantyMonths') IS NULL
    ALTER TABLE OrderDetails ADD WarrantyMonths INT NOT NULL CONSTRAINT DF_OrderDetails_WarrantyMonths DEFAULT 12;

IF OBJECT_ID('WarrantyRequests', 'U') IS NULL
BEGIN
    CREATE TABLE WarrantyRequests (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WarrantyRequests PRIMARY KEY,
        OrderId INT NULL, OrderDetailId INT NULL, ProductId INT NOT NULL, UserId INT NULL,
        CustomerName NVARCHAR(120) NOT NULL CONSTRAINT DF_WarrantyRequests_CustomerName DEFAULT '',
        Phone NVARCHAR(20) NOT NULL CONSTRAINT DF_WarrantyRequests_Phone DEFAULT '',
        Email NVARCHAR(120) NULL,
        ProductName NVARCHAR(200) NOT NULL CONSTRAINT DF_WarrantyRequests_ProductName DEFAULT '',
        WarrantyCode NVARCHAR(80) NOT NULL,
        SerialNumber NVARCHAR(100) NULL,
        PurchaseDate DATETIME2 NOT NULL CONSTRAINT DF_WarrantyRequests_PurchaseDate DEFAULT GETUTCDATE(),
        WarrantyMonths INT NOT NULL CONSTRAINT DF_WarrantyRequests_WarrantyMonths DEFAULT 12,
        IssueTitle NVARCHAR(200) NOT NULL CONSTRAINT DF_WarrantyRequests_IssueTitle DEFAULT '',
        IssueDescription NVARCHAR(2000) NOT NULL CONSTRAINT DF_WarrantyRequests_IssueDescription DEFAULT '',
        EvidencePath NVARCHAR(1000) NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_WarrantyRequests_Status DEFAULT N'Chờ tiếp nhận',
        AdminNote NVARCHAR(2000) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_WarrantyRequests_CreatedAt DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_WarrantyRequests_UpdatedAt DEFAULT GETUTCDATE()
    );
END
ELSE
BEGIN
    IF COL_LENGTH('WarrantyRequests', 'OrderId') IS NULL ALTER TABLE WarrantyRequests ADD OrderId INT NULL;
    IF COL_LENGTH('WarrantyRequests', 'OrderDetailId') IS NULL ALTER TABLE WarrantyRequests ADD OrderDetailId INT NULL;
    IF COL_LENGTH('WarrantyRequests', 'CustomerName') IS NULL ALTER TABLE WarrantyRequests ADD CustomerName NVARCHAR(120) NOT NULL CONSTRAINT DF_WarrantyRequests_CustomerName DEFAULT '';
    IF COL_LENGTH('WarrantyRequests', 'Phone') IS NULL ALTER TABLE WarrantyRequests ADD Phone NVARCHAR(20) NOT NULL CONSTRAINT DF_WarrantyRequests_Phone DEFAULT '';
    IF COL_LENGTH('WarrantyRequests', 'Email') IS NULL ALTER TABLE WarrantyRequests ADD Email NVARCHAR(120) NULL;
    IF COL_LENGTH('WarrantyRequests', 'ProductName') IS NULL ALTER TABLE WarrantyRequests ADD ProductName NVARCHAR(200) NOT NULL CONSTRAINT DF_WarrantyRequests_ProductName DEFAULT '';
    IF COL_LENGTH('WarrantyRequests', 'WarrantyCode') IS NULL ALTER TABLE WarrantyRequests ADD WarrantyCode NVARCHAR(80) NULL;
    IF COL_LENGTH('WarrantyRequests', 'SerialNumber') IS NULL ALTER TABLE WarrantyRequests ADD SerialNumber NVARCHAR(100) NULL;
    IF COL_LENGTH('WarrantyRequests', 'PurchaseDate') IS NULL ALTER TABLE WarrantyRequests ADD PurchaseDate DATETIME2 NOT NULL CONSTRAINT DF_WarrantyRequests_PurchaseDate DEFAULT GETUTCDATE();
    IF COL_LENGTH('WarrantyRequests', 'WarrantyMonths') IS NULL ALTER TABLE WarrantyRequests ADD WarrantyMonths INT NOT NULL CONSTRAINT DF_WarrantyRequests_WarrantyMonths DEFAULT 12;
    IF COL_LENGTH('WarrantyRequests', 'IssueTitle') IS NULL ALTER TABLE WarrantyRequests ADD IssueTitle NVARCHAR(200) NOT NULL CONSTRAINT DF_WarrantyRequests_IssueTitle DEFAULT N'Yêu cầu bảo hành';
    IF COL_LENGTH('WarrantyRequests', 'EvidencePath') IS NULL ALTER TABLE WarrantyRequests ADD EvidencePath NVARCHAR(1000) NULL;
    IF COL_LENGTH('WarrantyRequests', 'AdminNote') IS NULL ALTER TABLE WarrantyRequests ADD AdminNote NVARCHAR(2000) NULL;
    IF COL_LENGTH('WarrantyRequests', 'UpdatedAt') IS NULL ALTER TABLE WarrantyRequests ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_WarrantyRequests_UpdatedAt DEFAULT GETUTCDATE();
END
");

        // Separate batch so SQL Server resolves columns added above before compiling data migration statements.
        migrationBuilder.Sql(@"
UPDATE od SET WarrantyMonths = CASE WHEN p.WarrantyMonths > 0 THEN p.WarrantyMonths ELSE 12 END
FROM OrderDetails od INNER JOIN Products p ON p.Id = od.ProductId
WHERE od.WarrantyMonths IS NULL OR od.WarrantyMonths <= 0;

UPDATE wr SET
    CustomerName = COALESCE(NULLIF(wr.CustomerName, ''), u.FullName, o.ReceiverName, ''),
    Phone = COALESCE(NULLIF(wr.Phone, ''), u.Phone, o.ReceiverPhone, ''),
    Email = COALESCE(wr.Email, u.Email, o.CustomerEmail),
    ProductName = COALESCE(NULLIF(wr.ProductName, ''), p.Name, ''),
    WarrantyCode = COALESCE(NULLIF(wr.WarrantyCode, ''), CONCAT('BH-', FORMAT(wr.Id, '000000'))),
    Status = CASE WHEN wr.Status = N'Mới tạo' THEN N'Chờ tiếp nhận' ELSE wr.Status END,
    UpdatedAt = COALESCE(wr.UpdatedAt, wr.CreatedAt, GETUTCDATE())
FROM WarrantyRequests wr
LEFT JOIN Users u ON u.Id = wr.UserId
LEFT JOIN Products p ON p.Id = wr.ProductId
LEFT JOIN Orders o ON o.Id = wr.OrderId;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('WarrantyRequests') AND name = 'WarrantyCode' AND is_nullable = 1)
    ALTER TABLE WarrantyRequests ALTER COLUMN WarrantyCode NVARCHAR(80) NOT NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('WarrantyRequests') AND name = 'UserId' AND is_nullable = 0)
    ALTER TABLE WarrantyRequests ALTER COLUMN UserId INT NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('WarrantyRequests') AND name = 'IssueDescription' AND max_length < 4000)
    ALTER TABLE WarrantyRequests ALTER COLUMN IssueDescription NVARCHAR(2000) NOT NULL;
");

        migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_WarrantyRequests_Orders_OrderId')
    ALTER TABLE WarrantyRequests ADD CONSTRAINT FK_WarrantyRequests_Orders_OrderId FOREIGN KEY (OrderId) REFERENCES Orders(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_WarrantyRequests_OrderDetails_OrderDetailId')
    ALTER TABLE WarrantyRequests ADD CONSTRAINT FK_WarrantyRequests_OrderDetails_OrderDetailId FOREIGN KEY (OrderDetailId) REFERENCES OrderDetails(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_WarrantyRequests_Products_ProductId')
    ALTER TABLE WarrantyRequests ADD CONSTRAINT FK_WarrantyRequests_Products_ProductId FOREIGN KEY (ProductId) REFERENCES Products(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_WarrantyRequests_Users_UserId')
    ALTER TABLE WarrantyRequests ADD CONSTRAINT FK_WarrantyRequests_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WarrantyRequests_WarrantyCode' AND object_id = OBJECT_ID('WarrantyRequests'))
    CREATE INDEX IX_WarrantyRequests_WarrantyCode ON WarrantyRequests(WarrantyCode);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WarrantyRequests_Phone_CreatedAt' AND object_id = OBJECT_ID('WarrantyRequests'))
    CREATE INDEX IX_WarrantyRequests_Phone_CreatedAt ON WarrantyRequests(Phone, CreatedAt);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WarrantyRequests_Status_UpdatedAt' AND object_id = OBJECT_ID('WarrantyRequests'))
    CREATE INDEX IX_WarrantyRequests_Status_UpdatedAt ON WarrantyRequests(Status, UpdatedAt);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WarrantyRequests_OrderId' AND object_id = OBJECT_ID('WarrantyRequests'))
    CREATE INDEX IX_WarrantyRequests_OrderId ON WarrantyRequests(OrderId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WarrantyRequests_OrderDetailId' AND object_id = OBJECT_ID('WarrantyRequests'))
    CREATE INDEX IX_WarrantyRequests_OrderDetailId ON WarrantyRequests(OrderDetailId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WarrantyRequests_ProductId' AND object_id = OBJECT_ID('WarrantyRequests'))
    CREATE INDEX IX_WarrantyRequests_ProductId ON WarrantyRequests(ProductId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WarrantyRequests_UserId' AND object_id = OBJECT_ID('WarrantyRequests'))
    CREATE INDEX IX_WarrantyRequests_UserId ON WarrantyRequests(UserId);
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF OBJECT_ID('WarrantyRequests', 'U') IS NOT NULL DROP TABLE WarrantyRequests;
IF COL_LENGTH('OrderDetails', 'WarrantyMonths') IS NOT NULL
BEGIN
    DECLARE @constraintName NVARCHAR(200);
    SELECT @constraintName = dc.name FROM sys.default_constraints dc
    JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE c.object_id = OBJECT_ID('OrderDetails') AND c.name = 'WarrantyMonths';
    IF @constraintName IS NOT NULL EXEC('ALTER TABLE OrderDetails DROP CONSTRAINT [' + @constraintName + ']');
    ALTER TABLE OrderDetails DROP COLUMN WarrantyMonths;
END
");
    }
}
