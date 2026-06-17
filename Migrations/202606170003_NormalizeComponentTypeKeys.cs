using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datn.PcStore.Migrations
{
    public partial class NormalizeComponentTypeKeys : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE Products SET ComponentType = 'MAINBOARD' WHERE ComponentType IS NOT NULL AND (LOWER(ComponentType) LIKE '%mainboard%' OR LOWER(ComponentType) LIKE '%motherboard%' OR ComponentType LIKE N'%Bo mạch chủ%' OR ComponentType LIKE N'%Bo mach chu%');
UPDATE Products SET ComponentType = 'VGA' WHERE ComponentType IS NOT NULL AND (LOWER(ComponentType) LIKE '%vga%' OR LOWER(ComponentType) LIKE '%gpu%' OR ComponentType LIKE N'%Card màn hình%');
UPDATE Products SET ComponentType = 'STORAGE' WHERE ComponentType IS NOT NULL AND (LOWER(ComponentType) LIKE '%storage%' OR LOWER(ComponentType) IN ('ssd','hdd','ssd/hdd','ssd-hdd') OR ComponentType LIKE N'%Ổ cứng%' OR ComponentType LIKE N'%Ổ Cứng%');
UPDATE Products SET ComponentType = 'PSU' WHERE ComponentType IS NOT NULL AND (LOWER(ComponentType) LIKE '%psu%' OR ComponentType LIKE N'%Nguồn%');
UPDATE Products SET ComponentType = 'COOLER' WHERE ComponentType IS NOT NULL AND (LOWER(ComponentType) LIKE '%cooler%' OR ComponentType LIKE N'%Tản nhiệt%');
UPDATE Products SET ComponentType = 'CASE' WHERE ComponentType IS NOT NULL AND (LOWER(ComponentType) LIKE '%case%' OR ComponentType LIKE N'%Vỏ case%');
UPDATE Products SET ComponentType = 'MONITOR_ARM' WHERE ComponentType IS NOT NULL AND (LOWER(REPLACE(ComponentType, '_', '')) LIKE '%monitorarm%' OR ComponentType LIKE N'%Giá treo màn hình%');
UPDATE Products SET ComponentType = 'MONITOR' WHERE ComponentType IS NOT NULL AND ComponentType <> 'MONITOR_ARM' AND (LOWER(ComponentType) LIKE '%monitor%' OR ComponentType LIKE N'%Màn hình%');
UPDATE Products SET ComponentType = 'KEYBOARD' WHERE ComponentType IS NOT NULL AND (LOWER(ComponentType) LIKE '%keyboard%' OR ComponentType LIKE N'%Bàn phím%');
UPDATE Products SET ComponentType = 'MOUSE' WHERE ComponentType IS NOT NULL AND (LOWER(ComponentType) LIKE '%mouse%' OR ComponentType LIKE N'%Chuột%');
UPDATE Products SET ComponentType = 'HEADPHONE' WHERE ComponentType IS NOT NULL AND (LOWER(ComponentType) LIKE '%headphone%' OR LOWER(ComponentType) LIKE '%headset%' OR ComponentType LIKE N'%Tai nghe%');
UPDATE Products SET ComponentType = 'OTHER' WHERE ProductType = 'Component' AND (ComponentType IS NULL OR LTRIM(RTRIM(ComponentType)) = '' OR ComponentType = 'Other');
UPDATE ComponentBrands SET ComponentType = 'MAINBOARD' WHERE ComponentType IN ('Mainboard','Motherboard') OR ComponentType LIKE N'%Bo mạch chủ%';
UPDATE ComponentBrands SET ComponentType = 'STORAGE' WHERE ComponentType IN ('Storage','SSD','HDD','SSD/HDD','SSD-HDD') OR ComponentType LIKE N'%Ổ cứng%';
UPDATE ComponentBrands SET ComponentType = 'CASE' WHERE ComponentType = 'Case';
UPDATE ComponentBrands SET ComponentType = 'COOLER' WHERE ComponentType = 'Cooler';
UPDATE ComponentBrands SET ComponentType = 'MONITOR' WHERE ComponentType = 'Monitor';
UPDATE ComponentBrands SET ComponentType = 'KEYBOARD' WHERE ComponentType = 'Keyboard';
UPDATE ComponentBrands SET ComponentType = 'MOUSE' WHERE ComponentType = 'Mouse';
UPDATE ComponentBrands SET ComponentType = 'HEADPHONE' WHERE ComponentType IN ('Headphone','Headset');
UPDATE ComponentBrands SET ComponentType = 'MONITOR_ARM' WHERE ComponentType IN ('MonitorArm','Monitor Arm');
UPDATE ComponentBrands SET ComponentType = 'OTHER' WHERE ComponentType = 'Other';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
