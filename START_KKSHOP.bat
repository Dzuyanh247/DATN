@echo off
chcp 65001 >nul
cd /d "%~dp0"

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0START_KKSHOP.ps1"
if errorlevel 1 (
    echo.
    echo Khong khoi dong duoc KKSHOP. Vui long gui thu muc logs cho ky thuat.
    pause
    exit /b 1
)

pause
exit /b 0
