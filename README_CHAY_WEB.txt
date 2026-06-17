HUONG DAN CHAY WEBSITE KKSHOP CHO CHU SHOP
==========================================

1. Cach mo website
------------------
- Bam dup file START_KKSHOP.bat.
- Website se tu chay va tu mo trinh duyet tai:
  http://localhost:5000

2. Luu y khi dang ban hang
--------------------------
- Khong dong cua so CMD/PowerShell khi dang su dung website.
- Cua so nay la chuong trinh dang chay website.
- Log ky thuat duoc luu trong thu muc logs.

3. Cach tat website
-------------------
- Quay lai cua so CMD/PowerShell.
- Bam Enter de tat website.

4. Database migration
---------------------
- Mac dinh START_KKSHOP.config de RUN_MIGRATION=false.
- Chi doi thanh RUN_MIGRATION=true khi ky thuat yeu cau cap nhat database.

5. Neu bao loi
--------------
- Gui ca thu muc logs cho ky thuat.
- File log co dang:
  logs/startup-yyyyMMdd-HHmmss.log
  logs/runtime-yyyyMMdd-HHmmss.log

6. Loi thuong gap
-----------------
- Neu bao cong 5000 dang ban: co the website dang chay san. Chon Y neu muon tat phien ban cu va chay lai.
- Neu bao database loi: kiem tra SQL Server/SQLEXPRESS/LocalDB dang bat, sau do gui logs cho ky thuat.
- Neu bao restore/build that bai: gui logs cho ky thuat.
