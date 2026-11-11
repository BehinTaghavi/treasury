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

# On a slow or unreliable internet connection a download can break off in the middle of a build.
# Downloaded packages are kept (Docker build cache), so trying again only fetches what is missing.
function Invoke-Build {
    $attempts = 3
    for ($attempt = 1; $attempt -le $attempts; $attempt++) {
        & docker compose -f $compose --env-file $envFile build
        if ($LASTEXITCODE -eq 0) { return }
        if ($attempt -lt $attempts) {
            Write-Host ''
            Say "The build stopped, usually because a download broke off. Trying again (attempt $($attempt + 1) of $attempts); what was already downloaded is kept..."
        }
    }
    Fail "Docker could not build the system after $attempts attempts (see the lines above). Check the internet connection, then take a screenshot of this window and send it to Claude."
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

# Every setting the current version needs. A newer version may need more (e.g. a new service's password):
# missing ones are added to the existing file, existing ones are never changed (the database is locked with them).
function Get-RequiredSettings {
    return [ordered]@{
        'POSTGRES_PASSWORD'       = (New-Password)
        'CASHFLOW_DB_PASSWORD'    = (New-Password)
        'REFERENCE_DB_PASSWORD'   = (New-Password)
        'ACCOUNTS_DB_PASSWORD'    = (New-Password)
        'KEYCLOAK_ADMIN_PASSWORD' = (New-Password)
        'GATEWAY_PORT'            = '8080'
        'GATEWAY_ADDRESS'         = '127.0.0.1'
    }
}

function Initialize-Settings {
    $isNew = -not (Test-Path $envFile)
    if ($isNew) {
        New-Item -ItemType Directory -Force -Path $dataDir | Out-Null
        Set-Content -Path $envFile -Value '# Treasury test server settings. Keep this file: the test database is locked with these passwords.' -Encoding ASCII
        Say "Settings created: $envFile"
    }

    $existing = @{}
    foreach ($line in (Get-Content -Path $envFile)) {
        if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)=') { $existing[$Matches[1]] = $true }
    }
    $settings = Get-RequiredSettings
    $added = @()
    foreach ($name in $settings.Keys) {
        if (-not $existing.ContainsKey($name)) {
            Add-Content -Path $envFile -Value "$name=$($settings[$name])" -Encoding ASCII
            $added += $name
        }
    }
    if ($added.Count -gt 0 -and -not $isNew) { Say ('Settings added for this version: ' + ($added -join ', ')) }
}

function Get-Status([string]$url) {
    $ErrorActionPreference = 'Continue'
    $code = & $curl -s -o $nullDevice -w '%{http_code}' --max-time 5 $url 2> $null
    if (-not $code) { return '000' }
    return [string]$code
}

# A ZIP that was only partly extracted (stopped by antivirus or an error) gives an obscure Docker message; say it plainly.
function Test-Download {
    $missing = @('src', 'db', 'frontend', 'deploy') | Where-Object { -not (Test-Path ([IO.Path]::Combine($root, $_))) }
    if ($missing) {
        Fail ("The download was not completely extracted: the folder(s) " + ($missing -join ', ') + " are missing in $root. " +
            "Delete this folder, extract the ZIP again (guide: docs\test-server-windows.md, step 3) and check that 'src' is there.")
    }
}

function Start-Treasury {
    Test-Download
    Wait-ForDocker
    Initialize-Settings
    Say 'Building and starting the Treasury system. The first time this downloads about 2 GB and takes 10-20 minutes...'
    Invoke-Build
    Invoke-Compose @('up', '-d')

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
