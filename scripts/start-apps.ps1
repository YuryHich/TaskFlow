# Launch local desktop apps. Double-click start-apps.cmd or run:
#   powershell -ExecutionPolicy Bypass -File scripts/start-apps.ps1

$ErrorActionPreference = "Stop"

function Find-App([string[]]$candidates) {
    foreach ($path in $candidates) {
        if ($path -and (Test-Path -LiteralPath $path)) {
            return $path
        }
    }
    return $null
}

function Start-App {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string[]]$Candidates,
        [string]$FallbackCommand
    )

    $exe = Find-App $Candidates
    if ($exe) {
        Write-Host "Starting $Name..."
        Start-Process -FilePath $exe | Out-Null
        return
    }

    if ($FallbackCommand) {
        Write-Host "Starting $Name via PATH ($FallbackCommand)..."
        try {
            Start-Process -FilePath $FallbackCommand | Out-Null
            return
        }
        catch {
            # fall through
        }
    }

    Write-Warning "Skip $Name — executable not found. Install it or add it to PATH."
}

$local = $env:LOCALAPPDATA
$pf = ${env:ProgramFiles}
$pf86 = ${env:ProgramFiles(x86)}

Start-App -Name "Google Chrome" -FallbackCommand "chrome" -Candidates @(
    "$pf\Google\Chrome\Application\chrome.exe",
    "$pf86\Google\Chrome\Application\chrome.exe",
    "$local\Google\Chrome\Application\chrome.exe"
)

Start-App -Name "Cursor" -FallbackCommand "cursor" -Candidates @(
    "$local\Programs\cursor\Cursor.exe",
    "$local\cursor\Cursor.exe"
)

Start-App -Name "VS Code" -FallbackCommand "code" -Candidates @(
    "$local\Programs\Microsoft VS Code\Code.exe",
    "$pf\Microsoft VS Code\Code.exe"
)

Start-App -Name "Spotify" -FallbackCommand "spotify" -Candidates @(
    "$env:APPDATA\Spotify\Spotify.exe",
    "$local\Microsoft\WindowsApps\Spotify.exe"
)

Start-App -Name "WireGuard" -Candidates @(
    "$pf\WireGuard\wireguard.exe",
    "$pf86\WireGuard\wireguard.exe"
)

Start-App -Name "Docker Desktop" -Candidates @(
    "$pf\Docker\Docker\Docker Desktop.exe"
)

Write-Host "Done. Docker Engine can take 30-90 seconds after Docker Desktop opens."
