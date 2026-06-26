@echo off
setlocal EnableExtensions

cd /d "%~dp0Publish"

if not exist "Datn.PcStore.exe" (
    echo Không tìm thấy Datn.PcStore.exe. Vui lòng chạy PUBLISH_KKSHOP.bat trước.
    pause
    exit /b 1
)

start "" "Datn.PcStore.exe"
timeout /t 3 /nobreak >nul
start "" "http://localhost:5000"

exit /b 0
