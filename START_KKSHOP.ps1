$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot

$Url = 'http://localhost:5000'
$Port = 5000
$LogDir = Join-Path $PSScriptRoot 'logs'
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
$Stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$StartupLog = Join-Path $LogDir "startup-$Stamp.log"
$RuntimeLog = Join-Path $LogDir "runtime-$Stamp.log"
$ConfigFile = Join-Path $PSScriptRoot 'START_KKSHOP.config'

function Write-Ok([string]$Message) { Write-Host "[OK] $Message" -ForegroundColor Green }
function Write-ErrorLine([string]$Message) { Write-Host "[LOI] $Message" -ForegroundColor Red }
function Write-Info([string]$Message) { Write-Host $Message -ForegroundColor Cyan }
function Write-Log([string]$Message) { Add-Content -LiteralPath $StartupLog -Value "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] $Message" -Encoding UTF8 }
function Add-LogHeader([string]$Title) { Add-Content -LiteralPath $StartupLog -Value "`r`n===== $Title - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') =====" -Encoding UTF8 }
function Show-LogHelp {
    Write-Host 'Gui file log cho ky thuat:' -ForegroundColor Yellow
    Write-Host ("logs/{0}" -f (Split-Path -Leaf $StartupLog)) -ForegroundColor Yellow
    if (Test-Path -LiteralPath $RuntimeLog) { Write-Host ("logs/{0}" -f (Split-Path -Leaf $RuntimeLog)) -ForegroundColor Yellow }
}
function Stop-WithError([string]$Message, [int]$Code = 1) {
    Write-Log "ERROR: $Message"
    Write-ErrorLine $Message
    Show-LogHelp
    exit $Code
}
function Invoke-LoggedCommand([string]$Title, [string]$FileName, [string[]]$Arguments, [string]$FriendlyError) {
    Add-LogHeader $Title
    Write-Log "Run: $FileName $($Arguments -join ' ')"
    & $FileName @Arguments *>> $StartupLog
    $exitCode = $LASTEXITCODE
    Write-Log "ExitCode: $exitCode"
    if ($exitCode -ne 0) { Stop-WithError $FriendlyError }
}
function Get-ConfigValue([string]$Name, [string]$DefaultValue) {
    if (Test-Path -LiteralPath $ConfigFile) {
        $line = Get-Content -LiteralPath $ConfigFile -Encoding UTF8 | Where-Object { $_ -match "^\s*$Name\s*=" } | Select-Object -First 1
        if ($line) { return (($line -split '=', 2)[1]).Trim() }
    }
    return $DefaultValue
}
function Test-PortBusy([int]$PortNumber) {
    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $result = $client.BeginConnect('127.0.0.1', $PortNumber, $null, $null)
        if (-not $result.AsyncWaitHandle.WaitOne(300)) { return $false }
        $client.EndConnect($result)
        return $true
    } catch {
        return $false
    } finally {
        $client.Close()
    }
}
function Get-PortProcesses([int]$PortNumber) {
    try {
        $connections = Get-NetTCPConnection -LocalPort $PortNumber -State Listen -ErrorAction Stop
        return @($connections | Select-Object -ExpandProperty OwningProcess -Unique | ForEach-Object { Get-Process -Id $_ -ErrorAction SilentlyContinue })
    } catch {
        $lines = netstat -ano 2>$null | Select-String (":{0}\s+.*LISTENING" -f $PortNumber)
        $ids = @()
        foreach ($line in $lines) {
            $parts = ($line.ToString() -split '\s+') | Where-Object { $_ }
            if ($parts.Count -ge 5) { $ids += $parts[-1] }
        }
        return @($ids | Select-Object -Unique | ForEach-Object { Get-Process -Id $_ -ErrorAction SilentlyContinue })
    }
}
function Confirm-PortReady {
    if (-not (Test-PortBusy $Port)) {
        Write-Ok 'Cong 5000 san sang'
        return
    }

    Write-Host 'Cong 5000 dang duoc su dung.' -ForegroundColor Yellow
    $answer = Read-Host 'Ban co muon tat tien trinh cu khong? (Y/N)'
    if ($answer -notmatch '^[Yy]') { Stop-WithError 'Cong 5000 dang ban nen khong the khoi dong website.' }

    $processes = @(Get-PortProcesses $Port)
    if ($processes.Count -eq 0) { Stop-WithError 'Khong tim thay tien trinh dang dung cong 5000.' }
    foreach ($item in $processes) {
        Write-Log ("Stop process on port {0}: {1} ({2})" -f $Port, $item.ProcessName, $item.Id)
        Stop-Process -Id $item.Id -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 2
    if (Test-PortBusy $Port) { Stop-WithError 'Khong tat duoc tien trinh dang dung cong 5000.' }
    Write-Ok 'Cong 5000 san sang'
}
function Quote-Argument([string]$Value) {
    return '"' + ($Value -replace '"', '\"') + '"'
}
function Wait-WebsiteReady {
    for ($i = 1; $i -le 30; $i++) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) { return $true }
        } catch {
            Start-Sleep -Seconds 1
        }
    }
    return $false
}

try {
    Clear-Host
    Write-Info 'KKSHOP - TRINH KHOI DONG WEBSITE'
    Write-Host ''
    Write-Log 'Startup started.'

    & dotnet --version *>> $StartupLog
    if ($LASTEXITCODE -ne 0) { Stop-WithError 'Chua cai .NET. Vui long cai .NET SDK/Runtime truoc khi chay website.' }
    Write-Ok 'Da tim thay .NET'

    $project = Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.csproj' -File | Sort-Object Name | Select-Object -First 1
    if (-not $project) { Stop-WithError 'Khong tim thay file project .csproj.' }
    $projectPath = $project.FullName
    Write-Log "Project: $projectPath"
    Write-Ok 'Da kiem tra project'

    Confirm-PortReady

    Invoke-LoggedCommand 'dotnet restore' 'dotnet' @('restore', $projectPath) 'Restore that bai.'
    Write-Ok 'Restore thanh cong'

    Invoke-LoggedCommand 'dotnet build' 'dotnet' @('build', $projectPath, '--no-restore', '-c', 'Release') 'Build that bai.'
    Write-Ok 'Build thanh cong'

    $runMigration = (Get-ConfigValue 'RUN_MIGRATION' 'false') -ieq 'true'
    Write-Log "RUN_MIGRATION=$runMigration"
    if ($runMigration) {
        Invoke-LoggedCommand 'dotnet ef database update' 'dotnet' @('ef', 'database', 'update', '--project', $projectPath) 'Database loi hoac migration that bai.'
    } else {
        Write-Log 'Skip migration because RUN_MIGRATION=false.'
    }
    Write-Ok 'Database da san sang'

    Add-Content -LiteralPath $RuntimeLog -Value "===== dotnet run - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') =====" -Encoding UTF8
    $runArgs = @('run', '--project', (Quote-Argument $projectPath), '--no-build', '-c', 'Release', '--urls', $Url)
    Write-Log "Run website: dotnet $($runArgs -join ' ')"
    $process = Start-Process -FilePath 'dotnet' -ArgumentList $runArgs -WorkingDirectory $PSScriptRoot -RedirectStandardOutput $RuntimeLog -RedirectStandardError $RuntimeLog -PassThru
    if ($null -eq $process) { Stop-WithError 'Khong khoi dong duoc tien trinh website.' }

    Start-Sleep -Seconds 2
    if ($process.HasExited) { Stop-WithError 'Website khoi dong that bai.' }
    if (-not (Wait-WebsiteReady)) { Stop-WithError 'Website khoi dong that bai.' }

    try { Start-Process $Url | Out-Null } catch { Write-Log "Cannot open browser: $($_.Exception.Message)" }
    Write-Ok "Website da mo tai $Url"
    Write-Host ''
    Write-Host 'Website da san sang.' -ForegroundColor Green
    Write-Host 'Khong dong cua so nay neu dang su dung website.' -ForegroundColor Yellow
    Read-Host 'Bam Enter de tat website' | Out-Null

    if (($null -ne $process) -and (-not $process.HasExited)) {
        Write-Log "Stop website process: $($process.Id)"
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
    exit 0
} catch {
    Add-Content -LiteralPath $StartupLog -Value $_.Exception.ToString() -Encoding UTF8
    Write-ErrorLine 'Website khoi dong that bai.'
    Show-LogHelp
    exit 1
}
