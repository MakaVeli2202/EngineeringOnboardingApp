Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$paths = @(
    "$env:ProgramFiles\GitExtensions\GitExtensions.exe",
    "${env:ProgramFiles(x86)}\GitExtensions\GitExtensions.exe"
)

foreach ($path in $paths) {
    if (Test-Path $path) {
        Write-Output "GitExtensions is already installed."
        exit 0
    }
}

$winget = Get-Command winget -ErrorAction SilentlyContinue

if (-not $winget) {
    Write-Error "winget was not found."
    exit 1
}

Write-Output "Installing GitExtensions..."

winget install `
    --exact `
    --id GitExtensionsTeam.GitExtensions `
    --scope machine `
    --silent `
    --accept-package-agreements `
    --accept-source-agreements

if ($LASTEXITCODE -ne 0) {
    Write-Error "GitExtensions installation failed with code $LASTEXITCODE."
    exit $LASTEXITCODE
}

Write-Output "GitExtensions installed successfully."
exit 0