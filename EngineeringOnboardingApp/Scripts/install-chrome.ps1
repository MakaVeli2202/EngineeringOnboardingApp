Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$chromePaths = @(
    "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
    "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe"
)

foreach ($path in $chromePaths) {
    if (Test-Path $path) {
        Write-Output "Chrome is already installed."
        exit 0
    }
}

$winget = Get-Command winget -ErrorAction SilentlyContinue

if (-not $winget) {
    Write-Error "winget was not found. Install App Installer / WinGet first or replace this script with an internal installer source."
    exit 1
}

Write-Output "Installing Google Chrome..."

winget install `
    --exact `
    --id Google.Chrome `
    --scope machine `
    --silent `
    --accept-package-agreements `
    --accept-source-agreements

if ($LASTEXITCODE -ne 0) {
    Write-Error "Chrome installation failed with code $LASTEXITCODE."
    exit $LASTEXITCODE
}

Write-Output "Chrome installed successfully."
exit 0