Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$regPath = "HKCU:\Software\EngineeringOnboardingApp"

if (-not (Test-Path $regPath)) {
    New-Item -Path $regPath -Force | Out-Null
}

Set-ItemProperty `
    -Path $regPath `
    -Name "RegistryPlaceholderApplied" `
    -Value 1 `
    -Type DWord

Write-Output "Registry placeholder applied:"
Write-Output "HKCU\Software\EngineeringOnboardingApp\RegistryPlaceholderApplied = 1"

exit 0