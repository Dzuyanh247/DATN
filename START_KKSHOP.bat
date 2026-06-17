@echo off
setlocal EnableExtensions
cd /d "%~dp0"

title KKSHOP - Trinh khoi dong

where powershell >nul 2>&1
if errorlevel 1 (
    echo Khong tim thay PowerShell tren may nay.
    echo Vui long mo START_KKSHOP.ps1 bang PowerShell hoac lien he ky thuat.
    pause
    exit /b 1
)

if not exist "%~dp0START_KKSHOP.ps1" (
    echo Khong tim thay file START_KKSHOP.ps1.
    echo Vui long dat START_KKSHOP.bat va START_KKSHOP.ps1 trong cung thu muc project.
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0START_KKSHOP.ps1"
if errorlevel 1 (
    echo.
    echo PowerShell khong chay duoc script khoi dong.
    echo Vui long bam chuot phai START_KKSHOP.bat va chon Run as administrator neu Windows dang chan.
    pause
    exit /b 1
)

exit /b 0
