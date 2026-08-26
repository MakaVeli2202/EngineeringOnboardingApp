Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$paths = @(
    "$env:ProgramFiles\KDiff3\kdiff3.exe",
    "${env:ProgramFiles(x86)}\KDiff3\kdiff3.exe"
)

foreach ($path in $paths) {
    if (Test-Path $path) {
        Write-Output "KDiff3 is already installed."
        exit 0
    }
}

$winget = Get-Command winget -ErrorAction SilentlyContinue

if (-not $winget) {
    Write-Error "winget was not found."
    exit 1
}

Write-Output "Installing KDiff3..."

winget install `
    --exact `
    --id KDE.KDiff3 `
    --scope machine `
    --silent `
    --accept-package-agreements `
    --accept-source-agreements

if ($LASTEXITCODE -ne 0) {
    Write-Error "KDiff3 installation failed with code $LASTEXITCODE."
    exit $LASTEXITCODE
}

Write-Output "KDiff3 installed successfully."
exit 0