Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$paths = @(
    "$env:LocalAppData\Programs\Microsoft VS Code\Code.exe",
    "$env:ProgramFiles\Microsoft VS Code\Code.exe"
)

foreach ($path in $paths) {
    if (Test-Path $path) {
        Write-Output "Visual Studio Code is already installed."
        exit 0
    }
}

$winget = Get-Command winget -ErrorAction SilentlyContinue

if (-not $winget) {
    Write-Error "winget was not found."
    exit 1
}

Write-Output "Installing Visual Studio Code..."

winget install `
    --exact `
    --id Microsoft.VisualStudioCode `
    --scope machine `
    --silent `
    --accept-package-agreements `
    --accept-source-agreements

if ($LASTEXITCODE -ne 0) {
    Write-Error "Visual Studio Code installation failed with code $LASTEXITCODE."
    exit $LASTEXITCODE
}

Write-Output "Visual Studio Code installed successfully."
exit 0