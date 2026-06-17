HUONG DAN CHAY WEBSITE KKSHOP CHO CHU SHOP
==========================================

Muc dich
--------
File START_KKSHOP.bat giup chay website bang cach bam dup chuot, khong can go lenh dotnet run thu cong.

Buoc 1: Cai .NET Runtime/SDK dung phien ban
-------------------------------------------
Project dang dung .NET 8. Hay cai .NET 8 SDK hoac Runtime truoc khi chay.
Tai tai: https://dotnet.microsoft.com/download

Neu khong chac da cai chua, cu bam dup START_KKSHOP.bat. Neu may chua co .NET, chuong trinh se bao ro.

Buoc 2: Cai SQL Server neu website can database
-----------------------------------------------
Neu website dung database tren may tinh cua ban, hay cai mot trong cac ban sau:
- SQL Server LocalDB
- SQL Server Express
- SQL Server day du neu da co san

Sau do kiem tra connection string trong file appsettings.json cho dung ten server/database.

Buoc 3: Bam dup START_KKSHOP.bat
--------------------------------
Mo thu muc goc project KKSHOP, bam dup file:
START_KKSHOP.bat

File nay se tu dong:
1. Kiem tra .NET
2. Kiem tra file project .csproj
3. Restore package
4. Build project
5. Cap nhat database migration neu co dotnet-ef
6. Chay website

Buoc 4: Web tu mo tren trinh duyet
----------------------------------
Website mac dinh se mo tai:
http://localhost:5000

Neu trinh duyet khong tu mo, hay copy dia chi tren va dan vao Chrome/Edge.

Cach tat website
----------------
Khi muon tat website, lam mot trong hai cach:
- Dong cua so CMD/PowerShell dang chay website
- Hoac bam Ctrl+C trong cua so do, sau do xac nhan neu duoc hoi

Loi thuong gap va cach xu ly
---------------------------

1. Bao loi: Chua cai .NET
   Cach xu ly:
   - Cai .NET 8 SDK/Runtime tai https://dotnet.microsoft.com/download
   - Cai xong thi bam dup START_KKSHOP.bat lai

2. Bao loi: Chua cai dotnet-ef
   Cach xu ly:
   - Mo CMD hoac PowerShell
   - Chay lenh sau:
     dotnet tool install --global dotnet-ef
   - Cai xong thi bam dup START_KKSHOP.bat lai

3. Khong ket noi duoc database
   Cach xu ly:
   - Kiem tra SQL Server/LocalDB/SQLEXPRESS da chay chua
   - Kiem tra connection string trong appsettings.json
   - Kiem tra database da ton tai hay chua

4. Database chua dung cau truc hoac migration loi
   Cach xu ly:
   - Kiem tra thu muc Migrations cua project
   - Thu chay lai START_KKSHOP.bat sau khi cai dotnet-ef
   - Neu chi la database test, co the xoa DB test roi update lai

5. Cong website dang bi ung dung khac su dung
   Cach xu ly:
   - Tat ung dung dang chay tren cong 5000
   - Hoac doi port trong START_KKSHOP.ps1/START_KKSHOP.bat neu can

6. Build that bai
   Cach xu ly:
   - Doc loi mau do/loi hien trong cua so
   - Sua loi code/cau hinh theo thong bao
   - Cua so se khong tu tat, nen co the chup anh loi gui cho ky thuat vien

Ghi chu
-------
- Luon chay START_KKSHOP.bat tu thu muc goc project.
- File .bat da tu chuyen ve dung thu muc project, nen bam dup tu Windows Explorer van dung.
- Khi website chay thanh cong, khong dong cua so lenh neu van muon website tiep tuc hoat dong.
