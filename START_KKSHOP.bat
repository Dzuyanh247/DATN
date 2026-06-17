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
    echo File START_KKSHOP.ps1 dang bi loi cu phap. Vui long gui file logs cho ky thuat.
    echo Neu man hinh tren co duong dan log, hay gui dung file log do.
    pause
    exit /b 1
)

exit /b 0
