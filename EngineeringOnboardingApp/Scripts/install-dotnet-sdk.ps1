Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$appRoot = Split-Path -Parent $PSScriptRoot
$localInstaller = Join-Path $appRoot "Configs\Installers\dotnet-sdk-10.0.301-win-x64.exe"

try {
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue

    if ($dotnet) {
        $sdks = & dotnet --list-sdks 2>$null

        if ($sdks -match '^10\.') {
            Write-Output ".NET SDK 10.x appears to be installed already."
            exit 0
        }
    }
}
catch {
    Write-Output "Could not verify existing .NET SDK installation. Continuing with installer check."
}

if (-not (Test-Path $localInstaller)) {
    Write-Error "Local .NET SDK installer not found: $localInstaller"
    exit 1
}

Write-Output "Installing .NET SDK from local installer: $localInstaller"

$process = Start-Process `
    -FilePath $localInstaller `
    -ArgumentList "/install /quiet /norestart" `
    -Wait `
    -PassThru

Write-Output ".NET SDK installer exit code: $($process.ExitCode)"

if ($process.ExitCode -ne 0) {
    Write-Error ".NET SDK installation failed with exit code $($process.ExitCode)."
    exit $process.ExitCode
}

Write-Output ".NET SDK installation completed successfully."
exit 0