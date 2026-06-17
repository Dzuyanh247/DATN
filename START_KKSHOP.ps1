$ErrorActionPreference = 'Continue'
Set-Location -LiteralPath $PSScriptRoot
$Host.UI.RawUI.WindowTitle = 'KKSHOP - Khoi dong website'

function Write-Title {
    Write-Host '=====================================' -ForegroundColor Cyan
    Write-Host '        KKSHOP - KHOI DONG WEBSITE' -ForegroundColor Yellow
    Write-Host '=====================================' -ForegroundColor Cyan
    Write-Host ''
}

function Pause-And-Exit([int]$Code = 1) {
    Write-Host ''
    Read-Host 'Nhan Enter de dong cua so'
    exit $Code
}

function Run-Step([string]$Command, [string[]]$Arguments, [string]$FailMessage) {
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        Write-Host ''
        Write-Host $FailMessage -ForegroundColor Red
        Pause-And-Exit 1
    }
}

Write-Title
Write-Host '[1/6] Kiem tra .NET...' -ForegroundColor Green
& dotnet --version *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Chua cai .NET. Vui long cai .NET SDK/Runtime truoc khi chay website.' -ForegroundColor Red
    Write-Host 'Tai .NET tai: https://dotnet.microsoft.com/download' -ForegroundColor Yellow
    Pause-And-Exit 1
}
$dotnetVersion = (& dotnet --version)
Write-Host "Da tim thay .NET: $dotnetVersion" -ForegroundColor Green
Write-Host ''

Write-Host '[2/6] Kiem tra file project .csproj...' -ForegroundColor Green
$project = Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.csproj' -File | Select-Object -First 1
if (-not $project) {
    Write-Host "Khong tim thay file .csproj trong thu muc hien tai: $PSScriptRoot" -ForegroundColor Red
    Write-Host 'Hay dat START_KKSHOP.bat o thu muc goc project va chay lai.' -ForegroundColor Yellow
    Pause-And-Exit 1
}
Write-Host "Project: $($project.Name)" -ForegroundColor Green
Write-Host ''

Write-Host '[3/6] Restore package...' -ForegroundColor Green
Run-Step 'dotnet' @('restore', $project.FullName) 'Restore package that bai. Vui long xem loi ben tren. Kiem tra mang Internet hoac cau hinh NuGet.'
Write-Host ''

Write-Host '[4/6] Build project...' -ForegroundColor Green
& dotnet build $project.FullName --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host ''
    Write-Host 'Build that bai. Vui long xem loi ben tren.' -ForegroundColor Red
    Write-Host 'Neu thay loi lien quan database, hay kiem tra SQL Server/LocalDB/SQLEXPRESS va connection string trong appsettings.json.' -ForegroundColor Yellow
    Write-Host 'Neu thay loi lien quan cong/port, hay tat ung dung dang chiem cong hoac doi port.' -ForegroundColor Yellow
    Pause-And-Exit 1
}
Write-Host ''

Write-Host '[5/6] Cap nhat database migration neu co...' -ForegroundColor Green
& dotnet ef --version *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Chua cai dotnet-ef nen bo qua buoc update database.' -ForegroundColor Yellow
    Write-Host 'Neu website can migration, hay mo CMD/PowerShell va chay lenh:' -ForegroundColor Yellow
    Write-Host 'dotnet tool install --global dotnet-ef' -ForegroundColor Cyan
    Write-Host 'Sau do bam dup START_KKSHOP.bat lai.' -ForegroundColor Yellow
} else {
    & dotnet ef database update --project $project.FullName
    if ($LASTEXITCODE -ne 0) {
        Write-Host ''
        Write-Host 'Migration/database update that bai.' -ForegroundColor Red
        Write-Host 'Khong ket noi duoc database. Hay kiem tra SQL Server/LocalDB/SQLEXPRESS va connection string trong appsettings.json.' -ForegroundColor Yellow
        Write-Host 'Database chua dung cau truc. Hay kiem tra migration hoac xoa DB test roi update lai.' -ForegroundColor Yellow
        Pause-And-Exit 1
    }
}
Write-Host ''

$url = 'http://localhost:5000'
Write-Host '[6/6] Khoi dong website...' -ForegroundColor Green
Write-Host "Website se mo tai: $url" -ForegroundColor Cyan
Write-Host 'Neu muon tat web: dong cua so nay hoac bam Ctrl+C.' -ForegroundColor Yellow
Write-Host ''
$env:ASPNETCORE_URLS = $url
Start-Process $url
& dotnet run --project $project.FullName --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Host ''
    Write-Host 'Website dung do co loi.' -ForegroundColor Red
    Write-Host 'Khong ket noi duoc database. Hay kiem tra SQL Server/LocalDB/SQLEXPRESS va connection string trong appsettings.json.' -ForegroundColor Yellow
    Write-Host 'Cong website dang bi ung dung khac su dung. Hay tat ung dung do hoac doi port.' -ForegroundColor Yellow
    Write-Host 'Vui long xem log ben tren de biet chi tiet.' -ForegroundColor Yellow
    Pause-And-Exit 1
}

Write-Host ''
Write-Host 'Website da dung. Neu day la do ban bam Ctrl+C thi co the bo qua thong bao nay.' -ForegroundColor Yellow
Pause-And-Exit 0
