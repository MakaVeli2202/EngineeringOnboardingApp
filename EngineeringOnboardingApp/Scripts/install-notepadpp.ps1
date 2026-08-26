Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$paths = @(
    "$env:ProgramFiles\Notepad++\notepad++.exe",
    "${env:ProgramFiles(x86)}\Notepad++\notepad++.exe"
)

foreach ($path in $paths) {
    if (Test-Path $path) {
        Write-Output "Notepad++ is already installed."
        exit 0
    }
}

$winget = Get-Command winget -ErrorAction SilentlyContinue

if (-not $winget) {
    Write-Error "winget was not found."
    exit 1
}

Write-Output "Installing Notepad++..."

winget install `
    --exact `
    --id Notepad++.Notepad++ `
    --scope machine `
    --silent `
    --accept-package-agreements `
    --accept-source-agreements

if ($LASTEXITCODE -ne 0) {
    Write-Error "Notepad++ installation failed with code $LASTEXITCODE."
    exit $LASTEXITCODE
}

Write-Output "Notepad++ installed successfully."
exit 0