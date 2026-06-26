@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

set "CSPROJ=Datn.PcStore.csproj"
set "PACKAGE_DIR=ReleasePackage"
set "PUBLISH_DIR=%PACKAGE_DIR%\Publish"
set "PACKAGE_DB_DIR=%PACKAGE_DIR%\Database"
set "SOURCE_DB=Database\DATN_PCStore.sql"
set "PACKAGE_DB=%PACKAGE_DB_DIR%\DATN_PCStore.sql"

echo ========================================
echo KKSHOP - DONG GOI BAN CHAY LOCAL
echo ========================================
echo.

if not exist "%CSPROJ%" (
    echo Khong tim thay %CSPROJ%.
    echo Hay chay file nay o thu muc goc project KKSHOP.
    pause
    exit /b 1
)

if not exist "%PACKAGE_DIR%" mkdir "%PACKAGE_DIR%"
if not exist "%PUBLISH_DIR%" mkdir "%PUBLISH_DIR%"
if not exist "%PACKAGE_DB_DIR%" mkdir "%PACKAGE_DB_DIR%"

echo Kiem tra .NET...
dotnet --version >nul 2>nul
if errorlevel 1 (
    echo Chua cai .NET SDK. Vui long cai .NET 8 SDK.
    pause
    exit /b 1
)
dotnet --version
echo.

echo Restore package...
dotnet restore "%CSPROJ%"
if errorlevel 1 (
    echo Restore that bai. Hay xem loi ben tren.
    pause
    exit /b 1
)
echo.

echo Publish website vao %PUBLISH_DIR%...
dotnet publish "%CSPROJ%" -c Release -o "%PUBLISH_DIR%" --no-restore
if errorlevel 1 (
    echo Publish that bai. Hay xem loi ben tren.
    pause
    exit /b 1
)
echo.

echo Copy database script cho may local...
if exist "%SOURCE_DB%" (
    copy /Y "%SOURCE_DB%" "%PACKAGE_DB%" >nul
    if errorlevel 1 (
        echo Khong copy duoc %SOURCE_DB% sang %PACKAGE_DB%.
        pause
        exit /b 1
    )
    echo Da copy %SOURCE_DB% sang %PACKAGE_DB%.
) else (
    echo Khong tim thay %SOURCE_DB%.
    echo Bo qua buoc copy database script.
)
echo.

echo Hoan tat dong goi.
echo Luu y: cac file trong %PUBLISH_DIR% va %PACKAGE_DB% chi phuc vu local, khong commit len git.
echo De chay ban da publish, mo %PACKAGE_DIR%\START_KKSHOP.bat.
pause
exit /b 0
