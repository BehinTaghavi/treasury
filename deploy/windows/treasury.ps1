# Runs the Treasury system on this Windows PC with Docker Desktop (test server for the product owner).
# Called by the double-click files next to it: "Start Treasury.cmd", "Stop Treasury.cmd", "Reset test data.cmd".
#   start  - builds and starts everything, then opens the browser (first time: 10-20 minutes)
#   stop   - stops everything; the test data is kept
#   reset  - deletes all test data (asks first)
# Passwords are generated once and kept in %LOCALAPPDATA%\Treasury\treasury.env, outside the program folder,
# so a newer version can be unpacked anywhere and still finds the same data. Written for Windows PowerShell 5.1.
param([ValidateSet('start', 'stop', 'reset')][string]$Action = 'start')

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath([IO.Path]::Combine($PSScriptRoot, '..', '..'))
$compose = [IO.Path]::Combine($root, 'deploy', 'docker-compose.yml')
$appData = $env:LOCALAPPDATA
if (-not $appData) { $appData = $HOME }   # only for testing on Linux/macOS
$dataDir = [IO.Path]::Combine($appData, 'Treasury')
$envFile = [IO.Path]::Combine($dataDir, 'treasury.env')
$address = 'http://localhost:8080'

$onWindows = $env:OS -eq 'Windows_NT'
$curl = 'curl'
$nullDevice = '/dev/null'
if ($onWindows) { $curl = 'curl.exe'; $nullDevice = 'NUL' }   # 'curl' alone means Invoke-WebRequest in Windows PowerShell

function Say([string]$text) { Write-Host $text -ForegroundColor Cyan }

function Fail([string]$text) {
    Write-Host ''
    Write-Host $text -ForegroundColor Red
    exit 1
}

function Invoke-Compose([string[]]$arguments) {
    & docker compose -f $compose --env-file $envFile @arguments
    if ($LASTEXITCODE -ne 0) { Fail "Docker reported a problem (see the lines above). Take a screenshot of this window and send it to Claude." }
}

# Windows PowerShell 5.1 turns a program's error output into a script-stopping error when it is redirected,
# so the checks below run with 'Continue'.
function Test-Docker {
    $ErrorActionPreference = 'Continue'
    & docker info *> $null
    return ($LASTEXITCODE -eq 0)
}

function Wait-ForDocker {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        Fail "Docker Desktop is not installed. Install it first (guide: docs\test-server-windows.md, step 2)."
    }
    if (Test-Docker) { return }

    $desktop = ''
    if ($onWindows) { $desktop = Join-Path $env:ProgramFiles 'Docker\Docker\Docker Desktop.exe' }
    if ($onWindows -and (Test-Path $desktop)) {
        Say 'Starting Docker Desktop...'
        Start-Process $desktop
    }
    Say 'Waiting for Docker to be ready (up to 3 minutes)...'
    for ($i = 0; $i -lt 90; $i++) {
        Start-Sleep -Seconds 2
        if (Test-Docker) { return }
    }
    Fail "Docker is not running. Open Docker Desktop, wait until it says 'Engine running', then try again."
}

function New-Password { return ([guid]::NewGuid().ToString('N') + [guid]::NewGuid().ToString('N').Substring(0, 8)) }

function Initialize-Settings {
    if (Test-Path $envFile) { return }
    New-Item -ItemType Directory -Force -Path $dataDir | Out-Null
    $lines = @(
        '# Treasury test server settings. Created once; keep this file (the test database is locked with these passwords).',
        "POSTGRES_PASSWORD=$(New-Password)",
        "CASHFLOW_DB_PASSWORD=$(New-Password)",
        "KEYCLOAK_ADMIN_PASSWORD=$(New-Password)",
        'GATEWAY_PORT=8080',
        'GATEWAY_ADDRESS=127.0.0.1'
    )
    Set-Content -Path $envFile -Value $lines -Encoding ASCII
    Say "Settings created: $envFile"
}

function Get-Status([string]$url) {
    $ErrorActionPreference = 'Continue'
    $code = & $curl -s -o $nullDevice -w '%{http_code}' --max-time 5 $url 2> $null
    if (-not $code) { return '000' }
    return [string]$code
}

function Start-Treasury {
    Wait-ForDocker
    Initialize-Settings
    Say 'Building and starting the Treasury system. The first time this downloads about 2 GB and takes 10-20 minutes...'
    Invoke-Compose @('up', '-d', '--build')

    Say 'Waiting for the system to answer...'
    $ready = $false
    for ($i = 0; $i -lt 90; $i++) {
        # 404 for an unknown cheque means the program and its database are up.
        if ((Get-Status "$address/api/cashflow/instruments/00000000-0000-0000-0000-000000000000") -eq '404' -and (Get-Status "$address/") -eq '200') {
            $ready = $true
            break
        }
        Start-Sleep -Seconds 2
    }
    if (-not $ready) {
        Invoke-Compose @('ps', '-a')
        Fail "The system did not start in 3 minutes. Take a screenshot of this window and send it to Claude."
    }

    Write-Host ''
    Write-Host "Treasury is running at $address" -ForegroundColor Green
    Write-Host 'Opening the browser. You can close this window; the system keeps running until you use "Stop Treasury".'
    if ($onWindows) { Start-Process "$address/receipts/" }
}

function Stop-Treasury {
    Wait-ForDocker
    Initialize-Settings
    Say 'Stopping the Treasury system (your test data is kept)...'
    Invoke-Compose @('stop')
    Write-Host 'Stopped.' -ForegroundColor Green
}

function Reset-Treasury {
    Wait-ForDocker
    Initialize-Settings
    Write-Host 'This deletes ALL test data in the Treasury test system (cheques, cheque books, history).' -ForegroundColor Yellow
    $answer = Read-Host 'Type YES to delete everything'
    if ($answer -cne 'YES') {
        Write-Host 'Nothing was deleted.'
        return
    }
    Invoke-Compose @('down', '-v')
    Write-Host 'All test data deleted. Use "Start Treasury" to start again with an empty system.' -ForegroundColor Green
}

switch ($Action) {
    'start' { Start-Treasury }
    'stop' { Stop-Treasury }
    'reset' { Reset-Treasury }
}
