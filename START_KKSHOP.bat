@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

set "PORT=5000"
set "URL=http://localhost:5000"
set "CSPROJ=Datn.PcStore.csproj"

echo ========================================
echo KKSHOP - KHOI DONG WEBSITE
echo ========================================
echo.

echo Kiem tra .NET...
dotnet --version >nul 2>nul
if errorlevel 1 (
    echo.
    echo Chua cai .NET SDK/Runtime. Vui long cai .NET 8.
    echo.
    pause
    exit /b 1
)
dotnet --version
echo.

echo Kiem tra file project...
if not exist "%CSPROJ%" (
    echo.
    echo Khong tim thay file %CSPROJ%.
    echo Hay dat START_KKSHOP.bat trong dung thu muc project KKSHOP.
    echo.
    pause
    exit /b 1
)
echo Da tim thay %CSPROJ%.
echo.

echo Kiem tra cong %PORT%...
set "PORT_BUSY="
set "PID_LIST="
for /f "tokens=5" %%P in ('netstat -ano ^| findstr /R /C:":%PORT% .*LISTENING"') do (
    set "PORT_BUSY=1"
    echo !PID_LIST! | findstr /C:" %%P " >nul
    if errorlevel 1 set "PID_LIST=!PID_LIST! %%P "
)

if defined PORT_BUSY (
    echo Cong %PORT% dang duoc su dung.
    choice /C YN /M "Cong %PORT% dang duoc su dung. Ban co muon tat phien ban cu khong?"
    if errorlevel 2 (
        echo.
        echo Da dung khoi dong vi cong %PORT% dang ban.
        echo.
        pause
        exit /b 1
    )

    echo Dang tat phien ban cu tren cong %PORT%...
    for %%P in (!PID_LIST!) do (
        echo Tat PID %%P...
        taskkill /PID %%P /F
        if errorlevel 1 (
            echo.
            echo Khong tat duoc PID %%P. Vui long dong chuong trinh dang dung cong %PORT% roi chay lai.
            echo.
            pause
            exit /b 1
        )
    )
) else (
    echo Cong %PORT% san sang.
)
echo.

echo Restore package...
dotnet restore "%CSPROJ%"
if errorlevel 1 (
    echo.
    echo Restore that bai. Hay xem noi dung loi ben tren.
    echo.
    pause
    exit /b 1
)
echo Restore thanh cong.
echo.

echo Build website...
dotnet build "%CSPROJ%" --no-restore
if errorlevel 1 (
    echo.
    echo Build that bai. Hay xem noi dung loi ben tren.
    echo.
    pause
    exit /b 1
)
echo Build thanh cong. Neu chi co warning thi website van tiep tuc chay.
echo.

echo Mo trinh duyet: %URL%
start "" "%URL%"
echo.
echo Website dang chay tai %URL%
echo Khong dong cua so den nay khi dang su dung website.
echo Muon tat website: bam Ctrl+C roi chon Y, hoac dong cua so nay.
echo.

dotnet run --project "%CSPROJ%" --no-build --urls "%URL%"
if errorlevel 1 (
    echo.
    echo Website dung do co loi. Hay xem noi dung loi ben tren.
    echo.
    pause
    exit /b 1
)

echo.
echo Website da dung.
pause
exit /b 0
