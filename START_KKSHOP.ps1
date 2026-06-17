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

function Write-Step([string]$Message) { Write-Host $Message -ForegroundColor Cyan }
function Write-Ok([string]$Message) { Write-Host $Message -ForegroundColor Green }
function Write-Warn([string]$Message) { Write-Host $Message -ForegroundColor Yellow }
function Write-Fail([string]$Message) { Write-Host $Message -ForegroundColor Red }
function Write-Log([string]$Message) { Add-Content -LiteralPath $StartupLog -Value "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] $Message" -Encoding UTF8 }
function Add-LogHeader([string]$Text) { Add-Content -LiteralPath $StartupLog -Value "`r`n===== $Text - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') =====" -Encoding UTF8 }
function Pause-And-Exit([int]$Code = 1) {
    Write-Host ''
    if ($Host.Name -eq 'ConsoleHost') { Read-Host 'Bam Enter de dong cua so' | Out-Null }
    exit $Code
}
function Stop-With-FriendlyError([string]$Message, [string]$LogPath = $StartupLog) {
    Write-Fail $Message
    Write-Host "Vui long gui file log cho ky thuat: $LogPath" -ForegroundColor Yellow
    Pause-And-Exit 1
}
function Invoke-LoggedCommand([string]$Title, [string]$FileName, [string[]]$Arguments, [string]$FriendlyError) {
    Add-LogHeader $Title
    Write-Log "Run: $FileName $($Arguments -join ' ')"
    $output = & $FileName @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    $output | Out-File -FilePath $StartupLog -Append -Encoding UTF8
    if ($exitCode -ne 0) { Stop-With-FriendlyError $FriendlyError }
}
function Get-ConfigValue([string]$Name, [string]$DefaultValue) {
    $envValue = [Environment]::GetEnvironmentVariable($Name)
    if (-not [string]::IsNullOrWhiteSpace($envValue)) { return $envValue }
    if (Test-Path -LiteralPath $ConfigFile) {
        $line = Get-Content -LiteralPath $ConfigFile -Encoding UTF8 | Where-Object { $_ -match "^\s*$Name\s*=" } | Select-Object -First 1
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
    } catch {
        return $false
    } finally {
        $client.Close()
    }
}
function Get-PortProcesses([int]$PortNumber) {
    try {
        $connections = Get-NetTCPConnection -LocalPort $PortNumber -State Listen -ErrorAction Stop
        if (-not $connections) { return @() }
        return $connections | Select-Object -ExpandProperty OwningProcess -Unique | ForEach-Object { Get-Process -Id $_ -ErrorAction SilentlyContinue }
    } catch {
        $netstat = netstat -ano 2>$null | Select-String ":$PortNumber\s+.*LISTENING"
        $ids = @()
        foreach ($line in $netstat) {
            $parts = ($line.ToString() -split '\s+') | Where-Object { $_ }
            if ($parts.Count -ge 5) { $ids += $parts[-1] }
        }
        return $ids | Select-Object -Unique | ForEach-Object { Get-Process -Id $_ -ErrorAction SilentlyContinue }
    }
}
function Confirm-FreePort {
    if (-not (Test-PortOpen $Port)) { return }
    Write-Warn 'Cong 5000 dang duoc su dung. Ban co muon tat phien ban cu de chay lai khong? (Y/N)'
    $answer = Read-Host
    if ($answer -notmatch '^[Yy]') {
        Write-Log 'User chose not to stop the process using port 5000.'
        Pause-And-Exit 1
    }
    $processes = @(Get-PortProcesses $Port)
    if ($processes.Count -eq 0) { Stop-With-FriendlyError 'Khong tim thay process dang chiem cong 5000.' }
    foreach ($process in $processes) {
        Write-Log "Kill process on port ${Port}: $($process.ProcessName) ($($process.Id))"
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 2
    if (Test-PortOpen $Port) { Stop-With-FriendlyError 'Khong tat duoc ung dung dang chiem cong 5000.' }
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
function Join-CommandArguments([string[]]$Arguments) {
    return (($Arguments | ForEach-Object { '"' + ($_ -replace '"', '\"') + '"' }) -join ' ')
}
function Start-WebsiteProcess([string]$ProjectPath) {
    $env:ASPNETCORE_URLS = $Url
    $env:ASPNETCORE_ENVIRONMENT = 'Production'
    Add-LogHeader 'dotnet run'

    $dotnetArguments = @('run', '--project', $ProjectPath, '--no-build', '-c', 'Release', '--urls', $Url)
    $argumentText = Join-CommandArguments $dotnetArguments
    Write-Log "WorkingDirectory: $PSScriptRoot"
    Write-Log "ProjectPath: $ProjectPath"
    Write-Log "Url: $Url"
    Write-Log "Arguments: $argumentText"
    Add-Content -LiteralPath $RuntimeLog -Value "`r`n===== dotnet run - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') =====" -Encoding UTF8
    Add-Content -LiteralPath $RuntimeLog -Value "WorkingDirectory: $PSScriptRoot" -Encoding UTF8
    Add-Content -LiteralPath $RuntimeLog -Value "ProjectPath: $ProjectPath" -Encoding UTF8
    Add-Content -LiteralPath $RuntimeLog -Value "Url: $Url" -Encoding UTF8
    Add-Content -LiteralPath $RuntimeLog -Value "Arguments: $argumentText" -Encoding UTF8

    $process = $null
    try {
        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName = 'cmd.exe'
        $psi.Arguments = "/d /s /c `"dotnet $argumentText >> `"`"$RuntimeLog`"`" 2>>&1`""
        $psi.WorkingDirectory = $PSScriptRoot
        $psi.UseShellExecute = $false
        $psi.CreateNoWindow = $false
        $process = [System.Diagnostics.Process]::Start($psi)
    } catch {
        $message = "Khong khoi dong duoc website. Vui long xem file log runtime. $($_.Exception.Message)"
        Write-Log $message
        Add-Content -LiteralPath $RuntimeLog -Value $message -Encoding UTF8
        Stop-With-FriendlyError 'Khong khoi dong duoc website. Vui long xem file log runtime.' $RuntimeLog
    }

    if ($null -eq $process) {
        $message = 'Khong khoi dong duoc website. Process.Start tra ve null.'
        Write-Log $message
        Add-Content -LiteralPath $RuntimeLog -Value $message -Encoding UTF8
        Stop-With-FriendlyError 'Khong khoi dong duoc tien trinh website.' $RuntimeLog
    }

    return @{ Process = $process }
}

try {
    Clear-Host
    Write-Step 'Dang kiem tra moi truong...'
    Write-Log 'Startup started.'

    & dotnet --version *> $null
    if ($LASTEXITCODE -ne 0) { Stop-With-FriendlyError 'Chua cai .NET. Vui long cai .NET 8 SDK/Runtime truoc khi chay website.' }
    Write-Ok 'Da tim thay .NET'

    $project = Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.csproj' -File | Select-Object -First 1
    if (-not $project) { Stop-With-FriendlyError 'Khong tim thay file project .csproj.' }
    Write-Log "Project: $($project.FullName)"

    Confirm-FreePort

    Invoke-LoggedCommand 'dotnet restore' 'dotnet' @('restore', $project.FullName) 'Website chua restore duoc package.'
    Invoke-LoggedCommand 'dotnet build' 'dotnet' @('build', $project.FullName, '--no-restore', '-c', 'Release') 'Website chua build duoc.'

    Write-Step 'Dang kiem tra database...'
    $runMigration = (Get-ConfigValue 'RUN_MIGRATION' 'false') -ieq 'true'
    $env:RUN_MIGRATION = if ($runMigration) { 'true' } else { 'false' }
    Write-Log "RUN_MIGRATION=$($env:RUN_MIGRATION)"
    if ($runMigration) {
        & dotnet ef --version *> $null
        if ($LASTEXITCODE -ne 0) { Stop-With-FriendlyError 'Chua cai dotnet-ef nen khong cap nhat duoc database.' }
        Invoke-LoggedCommand 'dotnet ef database update' 'dotnet' @('ef', 'database', 'update', '--project', $project.FullName) 'Khong ket noi duoc database hoac cap nhat database that bai.'
    } else {
        Write-Log 'Skip migration. Set RUN_MIGRATION=true in START_KKSHOP.config to update database.'
    }

    Write-Step 'Website dang khoi dong...'
    $runtime = Start-WebsiteProcess $project.FullName
    Start-Sleep -Seconds 2
    if ($runtime.Process.HasExited) { Stop-With-FriendlyError 'Website khoi dong that bai.' $RuntimeLog }

    if (-not (Wait-WebsiteReady)) { Stop-With-FriendlyError 'Website khoi dong that bai.' $RuntimeLog }

    Write-Ok "Website da san sang tai: $Url"
    Write-Host 'Neu muon tat website: dong cua so nay hoac bam Ctrl+C' -ForegroundColor Yellow
    Write-Log 'Website is ready.'
    try { Start-Process $Url | Out-Null } catch { Write-Log "Cannot open browser: $($_.Exception.Message)" }

    [Console]::TreatControlCAsInput = $false
    [Console]::CancelKeyPress += {
        $EventArgs.Cancel = $true
        if ($runtime -and $runtime.Process -and -not $runtime.Process.HasExited) { $runtime.Process.Kill() }
    }
    $runtime.Process.WaitForExit()
} catch {
    Add-Content -LiteralPath $StartupLog -Value $_.Exception.ToString() -Encoding UTF8
    Write-Fail 'Website khoi dong that bai.'
    Write-Host "Vui long gui file log cho ky thuat: $StartupLog" -ForegroundColor Yellow
    Pause-And-Exit 1
}
