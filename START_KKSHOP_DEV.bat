@echo off
cd /d "%~dp0"

set "PORT=5000"
set "URL=http://localhost:5000"
set "CSPROJ=Datn.PcStore.csproj"

echo ========================================
echo KKSHOP - DEV MODE
echo ========================================
echo.

if not exist "%CSPROJ%" (
    echo Khong tim thay file %CSPROJ%.
    pause
    exit /b 1
)

echo Chay web truc tiep de dev xem log day du.
echo URL: %URL%
echo.

dotnet run --project "%CSPROJ%" --urls "%URL%"
if errorlevel 1 (
    echo.
    echo Website dung do co loi. Hay xem noi dung loi ben tren.
    echo.
    pause
    exit /b 1
)

pause
exit /b 0
