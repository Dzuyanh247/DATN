@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "PORT=5000"
set "PID_LIST="

echo Dang tim website KKSHOP tren cong %PORT%...
for /f "tokens=5" %%P in ('netstat -ano ^| findstr /R /C:":%PORT% .*LISTENING"') do (
    echo !PID_LIST! | findstr /C:" %%P " >nul
    if errorlevel 1 set "PID_LIST=!PID_LIST! %%P "
)

if not defined PID_LIST (
    echo Khong tim thay website dang chay tren cong %PORT%.
    pause
    exit /b 0
)

for %%P in (!PID_LIST!) do (
    echo Tat PID %%P...
    taskkill /PID %%P /F
)

echo Da gui lenh tat website KKSHOP.
pause
exit /b 0
