Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$appRoot = Split-Path -Parent $PSScriptRoot
$localInstaller = Join-Path $appRoot "Configs\Installers\winsdksetup.exe"

if (-not (Test-Path $localInstaller)) {
    Write-Error "Windows SDK installer not found: $localInstaller"
    exit 1
}

Write-Output "Installing Windows SDK from local installer: $localInstaller"

$process = Start-Process `
    -FilePath $localInstaller `
    -ArgumentList "/quiet /norestart" `
    -Wait `
    -PassThru

Write-Output "Windows SDK installer exit code: $($process.ExitCode)"

if ($process.ExitCode -ne 0) {
    Write-Error "Windows SDK installation failed with exit code $($process.ExitCode)."
    exit $process.ExitCode
}

Write-Output "Windows SDK installation completed successfully."
exit 0