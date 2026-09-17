# Bring up TaskFlow locally: compose, API, Vite, then Chrome windows.
# Double-click start-taskflow.cmd from this folder, or run:
#   powershell -ExecutionPolicy Bypass -File scripts/start-taskflow.ps1
#
# URLs:
#   SPA login     http://localhost:5173/login   (three windows)
#   Redis Insight http://localhost:5540
#   RabbitMQ UI   http://localhost:15672        (user taskflow / compose password)
#   pgAdmin       http://localhost:5050

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$webDir = Join-Path $repoRoot "web"
$loginUrl = "http://localhost:5173/login"
$redisInsightUrl = "http://localhost:5540"
$rabbitUrl = "http://localhost:15672"
$pgAdminUrl = "http://localhost:5050"

function Wait-Port {
    param(
        [Parameter(Mandatory)][string]$TargetHost,
        [Parameter(Mandatory)][int]$Port,
        [int]$TimeoutSec = 90,
        [string]$Label = "$TargetHost`:$Port"
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    Write-Host "Waiting for $Label ..."
    while ((Get-Date) -lt $deadline) {
        try {
            $client = New-Object System.Net.Sockets.TcpClient
            $iar = $client.BeginConnect($TargetHost, $Port, $null, $null)
            $ok = $iar.AsyncWaitHandle.WaitOne(800)
            if ($ok -and $client.Connected) {
                $client.EndConnect($iar)
                $client.Close()
                Write-Host "  $Label is up"
                return
            }
            $client.Close()
        }
        catch {
            # still starting
        }
        Start-Sleep -Milliseconds 700
    }
    throw "Timed out waiting for $Label"
}

function Wait-Http {
    param(
        [Parameter(Mandatory)][string]$Url,
        [int]$TimeoutSec = 90
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    Write-Host "Waiting for $Url ..."
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
                Write-Host "  $Url is up"
                return
            }
        }
        catch {
            # SPA/proxy may 404 until vite is ready; retry
        }
        Start-Sleep -Milliseconds 800
    }
    throw "Timed out waiting for $Url"
}

function Find-Chrome {
    $candidates = @(
        "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
        "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
        "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe"
    )
    foreach ($path in $candidates) {
        if (Test-Path -LiteralPath $path) { return $path }
    }
    $cmd = Get-Command chrome -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    throw "Google Chrome not found. Install Chrome or add chrome.exe to PATH."
}

function Open-ChromeWindow([string]$Url) {
    Start-Process -FilePath $script:chromeExe -ArgumentList @("--new-window", $Url) | Out-Null
}

Write-Host "Repo: $repoRoot"

Write-Host "Checking Docker Engine..."
$dockerReady = $false
for ($i = 0; $i -lt 60; $i++) {
    docker info 1>$null 2>$null
    if ($LASTEXITCODE -eq 0) {
        $dockerReady = $true
        break
    }
    Start-Sleep -Seconds 2
}
if (-not $dockerReady) {
    throw "Docker Engine is not ready. Start Docker Desktop (scripts\start-apps.cmd) and retry."
}

Write-Host "docker compose up -d ..."
Push-Location $repoRoot
try {
    docker compose up -d
    if ($LASTEXITCODE -ne 0) { throw "docker compose up failed" }
}
finally {
    Pop-Location
}

Wait-Port -TargetHost "127.0.0.1" -Port 5432 -Label "Postgres"
Wait-Port -TargetHost "127.0.0.1" -Port 6379 -Label "Redis"
Wait-Port -TargetHost "127.0.0.1" -Port 5672 -Label "RabbitMQ AMQP"
Wait-Port -TargetHost "127.0.0.1" -Port 5050 -Label "pgAdmin"
Wait-Port -TargetHost "127.0.0.1" -Port 5540 -Label "Redis Insight"
Wait-Port -TargetHost "127.0.0.1" -Port 15672 -Label "RabbitMQ management"

Write-Host "Starting API (new window)..."
Start-Process powershell -WorkingDirectory $repoRoot -ArgumentList @(
    "-NoExit",
    "-Command",
    "Set-Location '$repoRoot'; dotnet run --project API --launch-profile http"
) | Out-Null
Wait-Port -TargetHost "127.0.0.1" -Port 5031 -TimeoutSec 120 -Label "API"

Write-Host "Starting web (new window)..."
Start-Process powershell -WorkingDirectory $webDir -ArgumentList @(
    "-NoExit",
    "-Command",
    "Set-Location '$webDir'; npm run dev"
) | Out-Null
Wait-Http -Url $loginUrl -TimeoutSec 120

$script:chromeExe = Find-Chrome
Write-Host "Opening Chrome windows..."
1..3 | ForEach-Object { Open-ChromeWindow $loginUrl }
Open-ChromeWindow $redisInsightUrl
Open-ChromeWindow $rabbitUrl
Open-ChromeWindow $pgAdminUrl

Write-Host @"

Ready:
  API            http://localhost:5031
  SPA login x3   $loginUrl
  Redis Insight  $redisInsightUrl   (Redis host from Insight: taskflow-redis)
  RabbitMQ UI    $rabbitUrl         (taskflow / password from compose, unless you changed it)
  pgAdmin        $pgAdminUrl
"@
