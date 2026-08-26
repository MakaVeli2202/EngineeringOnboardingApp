Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$git = Get-Command git -ErrorAction SilentlyContinue

if ($git) {
    Write-Output "Git is already installed."
    exit 0
}

$winget = Get-Command winget -ErrorAction SilentlyContinue

if (-not $winget) {
    Write-Error "winget was not found. Install App Installer / WinGet first or replace this script with an internal installer source."
    exit 1
}

Write-Output "Installing Git..."

winget install `
    --exact `
    --id Git.Git `
    --scope machine `
    --silent `
    --accept-package-agreements `
    --accept-source-agreements

if ($LASTEXITCODE -ne 0) {
    Write-Error "Git installation failed with code $LASTEXITCODE."
    exit $LASTEXITCODE
}

Write-Output "Git installed successfully."
exit 0