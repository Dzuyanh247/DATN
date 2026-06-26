@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

set "PORT=5000"
set "URL=http://localhost:5000"
set "PUBLISH_DIR=Publish"
set "APP_DLL=Datn.PcStore.dll"

echo ========================================
echo KKSHOP - KHOI DONG BAN DA PUBLISH
echo ========================================
echo.

if not exist "%PUBLISH_DIR%\%APP_DLL%" (
    echo Khong tim thay %PUBLISH_DIR%\%APP_DLL%.
    echo Hay chay ..\PUBLISH_KKSHOP.bat truoc de tao file publish local.
    pause
    exit /b 1
)

dotnet --version >nul 2>nul
if errorlevel 1 (
    echo Chua cai .NET Runtime. Vui long cai .NET 8 Runtime.
    pause
    exit /b 1
)

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
    choice /C YN /M "Ban co muon tat phien ban cu khong?"
    if errorlevel 2 exit /b 1
    for %%P in (!PID_LIST!) do taskkill /PID %%P /F
)

echo Mo trinh duyet: %URL%
start "" "%URL%"
echo Website dang chay tai %URL%.
echo Muon tat website: chay STOP_KKSHOP.bat hoac bam Ctrl+C roi chon Y.
echo.

dotnet "%PUBLISH_DIR%\%APP_DLL%" --urls "%URL%"
if errorlevel 1 (
    echo Website dung do co loi. Hay xem noi dung loi ben tren.
    pause
    exit /b 1
)

pause
exit /b 0
