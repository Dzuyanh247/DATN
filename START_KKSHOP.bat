@echo off
setlocal EnableExtensions EnableDelayedExpansion

REM Luon chay tu thu muc chua file .bat, ke ca khi bam dup trong Windows Explorer.
cd /d "%~dp0"

if exist "%~dp0START_KKSHOP.ps1" (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0START_KKSHOP.ps1"
    exit /b %ERRORLEVEL%
)

title KKSHOP - Khoi dong website
color 0A

echo =====================================
echo         KKSHOP - KHOI DONG WEBSITE
echo =====================================
echo.

echo [1/6] Kiem tra .NET...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo.
    echo Chua cai .NET. Vui long cai .NET SDK/Runtime truoc khi chay website.
    echo Tai .NET tai: https://dotnet.microsoft.com/download
    echo.
    pause
    exit /b 1
)
for /f "usebackq delims=" %%v in (`dotnet --version`) do set DOTNET_VERSION=%%v
echo Da tim thay .NET: !DOTNET_VERSION!
echo.

echo [2/6] Kiem tra file project .csproj...
set CSPROJ=
for %%f in (*.csproj) do (
    if not defined CSPROJ set CSPROJ=%%f
)
if not defined CSPROJ (
    echo.
    echo Khong tim thay file .csproj trong thu muc hien tai:
    echo %CD%
    echo Hay dat START_KKSHOP.bat o thu muc goc project va chay lai.
    echo.
    pause
    exit /b 1
)
echo Project: !CSPROJ!
echo.

echo [3/6] Restore package...
dotnet restore "!CSPROJ!"
if errorlevel 1 (
    echo.
    echo Restore package that bai. Vui long xem loi ben tren.
    echo Kiem tra mang Internet hoac cau hinh NuGet.
    echo.
    pause
    exit /b 1
)
echo.

echo [4/6] Build project...
dotnet build "!CSPROJ!" --no-restore
if errorlevel 1 (
    echo.
    echo Build that bai. Vui long xem loi ben tren.
    echo Neu thay loi lien quan database, hay kiem tra SQL Server/LocalDB/SQLEXPRESS va connection string trong appsettings.json.
    echo Neu thay loi lien quan cong/port, hay tat ung dung dang chiem cong hoac doi port.
    echo.
    pause
    exit /b 1
)
echo.

echo [5/6] Cap nhat database migration neu co...
dotnet ef --version >nul 2>&1
if errorlevel 1 (
    echo.
    echo Chua cai dotnet-ef nen bo qua buoc update database.
    echo Neu website can migration, hay mo CMD/PowerShell va chay lenh:
    echo dotnet tool install --global dotnet-ef
    echo Sau do bam dup START_KKSHOP.bat lai.
    echo.
) else (
    dotnet ef database update --project "!CSPROJ!"
    if errorlevel 1 (
        echo.
        echo Migration/database update that bai.
        echo Khong ket noi duoc database. Hay kiem tra SQL Server/LocalDB/SQLEXPRESS va connection string trong appsettings.json.
        echo Database chua dung cau truc. Hay kiem tra migration hoac xoa DB test roi update lai.
        echo.
        pause
        exit /b 1
    )
)
echo.

echo [6/6] Khoi dong website...
echo Website se mo tai: http://localhost:5000
echo Neu muon tat web: dong cua so nay hoac bam Ctrl+C.
echo.
set ASPNETCORE_URLS=http://localhost:5000
start "" "http://localhost:5000"
dotnet run --project "!CSPROJ!" --no-build
if errorlevel 1 (
    echo.
    echo Website dung do co loi.
    echo Khong ket noi duoc database. Hay kiem tra SQL Server/LocalDB/SQLEXPRESS va connection string trong appsettings.json.
    echo Cong website dang bi ung dung khac su dung. Hay tat ung dung do hoac doi port.
    echo Vui long xem log ben tren de biet chi tiet.
    echo.
    pause
    exit /b 1
)

echo.
echo Website da dung. Neu day la do ban bam Ctrl+C thi co the bo qua thong bao nay.
pause
exit /b 0
