$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
$Host.UI.RawUI.WindowTitle = 'KKSHOP - Trinh khoi dong'

$Url = 'http://localhost:5000'
$Port = 5000
$LogDir = Join-Path $PSScriptRoot 'logs'
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
$Stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$StartupLog = Join-Path $LogDir "startup-$Stamp.log"
$RuntimeLog = Join-Path $LogDir "runtime-$Stamp.log"
$ConfigFile = Join-Path $PSScriptRoot 'START_KKSHOP.config'

function Write-Title {
    Clear-Host
    Write-Host '========================================' -ForegroundColor Cyan
    Write-Host '        KKSHOP - TRÌNH KHỞI ĐỘNG' -ForegroundColor Yellow
    Write-Host '========================================' -ForegroundColor Cyan
    Write-Host ''
}
function Write-Success([string]$Message) { Write-Host "[✓] $Message" -ForegroundColor Green }
function Write-WarningMessage([string]$Message) { Write-Host "[!] $Message" -ForegroundColor Yellow }
function Write-ErrorMessage([string]$Message) { Write-Host "[X] $Message" -ForegroundColor Red }
function Pause-And-Exit([int]$Code = 1) {
    Write-Host ''
    Read-Host 'Nhấn Enter để đóng cửa sổ'
    exit $Code
}
function Add-LogHeader([string]$Text) {
    Add-Content -LiteralPath $StartupLog -Value "`r`n===== $Text - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ====="
}
function Invoke-LoggedCommand([string]$Title, [string]$FileName, [string[]]$Arguments, [string]$FriendlyError) {
    Add-LogHeader $Title
    $output = & $FileName @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    $output | Out-File -FilePath $StartupLog -Append -Encoding UTF8
    if ($exitCode -ne 0) {
        Write-ErrorMessage $FriendlyError
        Write-Host "Vui lòng gửi file log này cho kỹ thuật: $StartupLog" -ForegroundColor Yellow
        Pause-And-Exit 1
    }
}
function Get-ConfigValue([string]$Name, [string]$DefaultValue) {
    $envValue = [Environment]::GetEnvironmentVariable($Name)
    if ($envValue) { return $envValue }
    if (Test-Path -LiteralPath $ConfigFile) {
        $line = Get-Content -LiteralPath $ConfigFile | Where-Object { $_ -match "^\s*$Name\s*=" } | Select-Object -First 1
        if ($line) { return (($line -split '=', 2)[1]).Trim() }
    }
    return $DefaultValue
}
function Test-PortOpen([int]$PortNumber) {
    $client = New-Object Net.Sockets.TcpClient
    try {
        $result = $client.BeginConnect('127.0.0.1', $PortNumber, $null, $null)
        if (-not $result.AsyncWaitHandle.WaitOne(300)) { return $false }
        $client.EndConnect($result)
        return $true
    } catch { return $false }
    finally { $client.Close() }
}
function Get-PortProcesses([int]$PortNumber) {
    $connections = Get-NetTCPConnection -LocalPort $PortNumber -State Listen -ErrorAction SilentlyContinue
    if (-not $connections) { return @() }
    return $connections | Select-Object -ExpandProperty OwningProcess -Unique | ForEach-Object { Get-Process -Id $_ -ErrorAction SilentlyContinue }
}
function Confirm-FreePort {
    if (-not (Test-PortOpen $Port)) { return }
    Write-WarningMessage 'Website có vẻ đang chạy rồi hoặc cổng 5000 đang bận.'
    $answer = Read-Host 'Bạn muốn tắt phiên bản cũ và chạy lại không? (Y/N)'
    if ($answer -notmatch '^[Yy]') { Pause-And-Exit 1 }
    foreach ($process in Get-PortProcesses $Port) {
        Add-Content -LiteralPath $StartupLog -Value "Killing process on port $Port: $($process.ProcessName) ($($process.Id))"
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 2
    if (Test-PortOpen $Port) {
        Write-ErrorMessage 'Không tắt được ứng dụng đang chiếm cổng 5000.'
        Write-Host "Vui lòng gửi file log này cho kỹ thuật: $StartupLog" -ForegroundColor Yellow
        Pause-And-Exit 1
    }
}
function Wait-WebsiteReady {
    for ($i = 1; $i -le 20; $i++) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) { return $true }
        } catch {
            Start-Sleep -Seconds 1
        }
    }
    return $false
}
function Start-WebsiteProcess([string]$ProjectPath) {
    $env:ASPNETCORE_URLS = $Url
    $env:ASPNETCORE_ENVIRONMENT = 'Production'
    Add-LogHeader 'dotnet run'
    $psi = [System.Diagnostics.ProcessStartInfo]::new('dotnet')
    foreach ($arg in @('run', '--project', $ProjectPath, '--no-build', '-c', 'Release', '--urls', $Url)) { [void]$psi.ArgumentList.Add($arg) }
    $psi.WorkingDirectory = $PSScriptRoot
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $process = [System.Diagnostics.Process]::Start($psi)
    $outWriter = [System.IO.StreamWriter]::new($RuntimeLog, $true, [System.Text.UTF8Encoding]::new($false))
    $errWriter = [System.IO.StreamWriter]::new($RuntimeLog, $true, [System.Text.UTF8Encoding]::new($false))
    $process.add_OutputDataReceived({ if ($EventArgs.Data) { $outWriter.WriteLine($EventArgs.Data); $outWriter.Flush() } })
    $process.add_ErrorDataReceived({ if ($EventArgs.Data) { $errWriter.WriteLine($EventArgs.Data); $errWriter.Flush() } })
    $process.BeginOutputReadLine(); $process.BeginErrorReadLine()
    return @{ Process = $process; OutWriter = $outWriter; ErrWriter = $errWriter }
}

try {
    Write-Title
    Write-Host "Log kỹ thuật: $StartupLog" -ForegroundColor DarkGray
    Write-Host ''

    Write-Host '1. Chạy website' -ForegroundColor White
    Write-Host '2. Cập nhật database rồi chạy website' -ForegroundColor White
    Write-Host '3. Thoát' -ForegroundColor White
    $choice = Read-Host 'Chọn chức năng (Enter = 1)'
    if ([string]::IsNullOrWhiteSpace($choice)) { $choice = '1' }
    if ($choice -eq '3') { exit 0 }
    if ($choice -notin @('1','2')) { Write-WarningMessage 'Lựa chọn không hợp lệ, mặc định chạy website.'; $choice = '1' }
    Write-Host ''

    & dotnet --version *> $null
    if ($LASTEXITCODE -ne 0) {
        Write-ErrorMessage 'Chưa cài .NET. Vui lòng cài .NET 8 SDK/Runtime trước khi chạy website.'
        Write-Host 'Tải tại: https://dotnet.microsoft.com/download' -ForegroundColor Yellow
        Pause-And-Exit 1
    }
    Write-Success 'Đã kiểm tra .NET'

    $project = Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.csproj' -File | Select-Object -First 1
    if (-not $project) {
        Write-ErrorMessage 'Không tìm thấy file project .csproj.'
        Write-Host 'Hãy đặt START_KKSHOP.bat ở thư mục gốc project và chạy lại.' -ForegroundColor Yellow
        Pause-And-Exit 1
    }
    Write-Success 'Đã kiểm tra project'

    Confirm-FreePort

    Invoke-LoggedCommand 'dotnet restore' 'dotnet' @('restore', $project.FullName) 'Website chưa restore được package.'
    Invoke-LoggedCommand 'dotnet build' 'dotnet' @('build', $project.FullName, '--no-restore', '-c', 'Release') 'Website chưa build được.'

    $runMigration = ((Get-ConfigValue 'RUN_MIGRATION' 'false') -ieq 'true') -or ($choice -eq '2')
    $env:RUN_MIGRATION = if ($runMigration) { 'true' } else { 'false' }
    if ($runMigration) {
        & dotnet ef --version *> $null
        if ($LASTEXITCODE -ne 0) {
            Write-ErrorMessage 'Chưa cài dotnet-ef nên không cập nhật được database.'
            Write-Host "Vui lòng gửi file log này cho kỹ thuật: $StartupLog" -ForegroundColor Yellow
            Pause-And-Exit 1
        }
        Invoke-LoggedCommand 'dotnet ef database update' 'dotnet' @('ef', 'database', 'update', '--project', $project.FullName) 'Không kết nối được database hoặc cập nhật database thất bại.'
    }
    Write-Success 'Đã kiểm tra database'

    Write-Success 'Website đã sẵn sàng'
    Write-Host ''
    Write-Host 'Đang mở website...' -ForegroundColor Cyan
    Write-Host "Địa chỉ: $Url" -ForegroundColor Cyan
    Write-Host ''

    $runtime = Start-WebsiteProcess $project.FullName
    Start-Sleep -Seconds 2
    if ($runtime.Process.HasExited) {
        Write-ErrorMessage 'Website khởi động thất bại.'
        Write-Host 'Không kết nối được database hoặc website gặp lỗi khi chạy.' -ForegroundColor Yellow
        Write-Host 'Vui lòng kiểm tra SQL Server đang bật.' -ForegroundColor Yellow
        Write-Host "Chi tiết lỗi đã lưu trong file: $RuntimeLog" -ForegroundColor Yellow
        Pause-And-Exit 1
    }

    if (Wait-WebsiteReady) {
        Start-Process $Url
        Write-Success 'Website đã mở thành công.'
    } else {
        Write-WarningMessage 'Website chưa phản hồi sau 20 giây.'
        Write-Host "Chi tiết lỗi đã lưu trong file: $RuntimeLog" -ForegroundColor Yellow
    }

    Write-Host ''
    Write-Host 'Website đang chạy...' -ForegroundColor Green
    Write-Host 'Không đóng cửa sổ này khi đang sử dụng web.' -ForegroundColor Yellow
    Write-Host 'Nhấn Ctrl + C để tắt.' -ForegroundColor Yellow
    Write-Host "Log runtime: $RuntimeLog" -ForegroundColor DarkGray

    [Console]::TreatControlCAsInput = $false
    [Console]::CancelKeyPress += {
        $EventArgs.Cancel = $true
        if (-not $runtime.Process.HasExited) { $runtime.Process.Kill() }
    }
    $runtime.Process.WaitForExit()
    $runtime.OutWriter.Dispose(); $runtime.ErrWriter.Dispose()
} catch {
    Add-Content -LiteralPath $StartupLog -Value $_.Exception.ToString()
    Write-ErrorMessage 'Website khởi động thất bại.'
    Write-Host "Vui lòng gửi file log này cho kỹ thuật: $StartupLog" -ForegroundColor Yellow
    Pause-And-Exit 1
}
