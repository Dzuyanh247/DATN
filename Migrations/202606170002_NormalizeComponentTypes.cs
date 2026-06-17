using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations
{
    public partial class NormalizeComponentTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE Products SET ComponentType = 'CPU' WHERE ComponentType IS NOT NULL AND (LOWER(LTRIM(RTRIM(ComponentType))) = 'cpu' OR ComponentType LIKE N'%Bộ vi xử lý%');
UPDATE Products SET ComponentType = 'Mainboard' WHERE ComponentType IS NOT NULL AND (LOWER(ComponentType) LIKE '%mainboard%' OR ComponentType LIKE N'%Bo mạch chủ%' OR ComponentType LIKE N'%Bo mach chu%' OR LOWER(ComponentType) LIKE '%motherboard%');
UPDATE Products SET ComponentType = 'RAM' WHERE ComponentType IS NOT NULL AND (LOWER(LTRIM(RTRIM(ComponentType))) = 'ram' OR ComponentType LIKE N'%Bộ nhớ trong%');
UPDATE Products SET ComponentType = 'VGA' WHERE ComponentType IS NOT NULL AND (LOWER(ComponentType) LIKE '%vga%' OR LOWER(ComponentType) LIKE '%gpu%' OR ComponentType LIKE N'%Card màn hình%');
UPDATE Products SET ComponentType = 'Storage' WHERE ComponentType IS NOT NULL AND (LOWER(ComponentType) LIKE '%storage%' OR LOWER(ComponentType) IN ('ssd','hdd','ssd/hdd') OR ComponentType LIKE N'%Ổ cứng%');
UPDATE Products SET ComponentType = 'PSU' WHERE ComponentType IS NOT NULL AND (LOWER(ComponentType) LIKE '%psu%' OR ComponentType LIKE N'%Nguồn%');
UPDATE Products SET ComponentType = 'Case' WHERE ComponentType IS NOT NULL AND (LOWER(ComponentType) LIKE '%case%' OR ComponentType LIKE N'%Vỏ case%');
UPDATE Products SET ComponentType = 'Cooler' WHERE ComponentType IS NOT NULL AND (LOWER(ComponentType) LIKE '%cooler%' OR ComponentType LIKE N'%Tản nhiệt%');
UPDATE Products SET ComponentType = 'MonitorArm' WHERE ComponentType IS NOT NULL AND (LOWER(ComponentType) LIKE '%monitorarm%' OR ComponentType LIKE N'%Giá treo màn hình%');
UPDATE Products SET ComponentType = 'Monitor' WHERE ComponentType IS NOT NULL AND ComponentType <> 'MonitorArm' AND (LOWER(ComponentType) LIKE '%monitor%' OR ComponentType LIKE N'%Màn hình%');
UPDATE Products SET ComponentType = 'Keyboard' WHERE ComponentType IS NOT NULL AND (LOWER(ComponentType) LIKE '%keyboard%' OR ComponentType LIKE N'%Bàn phím%');
UPDATE Products SET ComponentType = 'Mouse' WHERE ComponentType IS NOT NULL AND (LOWER(ComponentType) LIKE '%mouse%' OR ComponentType LIKE N'%Chuột%');
UPDATE Products SET ComponentType = 'Headphone' WHERE ComponentType IS NOT NULL AND (LOWER(ComponentType) LIKE '%headphone%' OR LOWER(ComponentType) LIKE '%headset%' OR ComponentType LIKE N'%Tai nghe%');
UPDATE Products SET ComponentType = 'Other' WHERE ProductType = 'Component' AND (ComponentType IS NULL OR LTRIM(RTRIM(ComponentType)) = '');
UPDATE Products SET ProductType = 'Component'
WHERE ProductType IS NULL
  AND ComponentType IN ('CPU','Mainboard','RAM','VGA','Storage','PSU','Case','Cooler','Monitor','Keyboard','Mouse','Headphone','MonitorArm','Other');
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
