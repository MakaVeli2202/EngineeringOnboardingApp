param(
    [ValidateSet("2022", "2026")]
    [string]$Version = "2026",

    [ValidateSet("Professional", "Enterprise")]
    [string]$Edition = "Professional"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$appRoot = Split-Path -Parent $PSScriptRoot
$installerDir = Join-Path $appRoot "Configs\Installers"
$tempDir = Join-Path $env:TEMP "EngineeringOnboardingApp"
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

function Test-IsAdministrator {
    $currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentIdentity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Test-InternetConnectivity {
    try {
        return [bool](Test-NetConnection -ComputerName "aka.ms" -Port 443 -InformationLevel Quiet -WarningAction SilentlyContinue)
    }
    catch {
        return $false
    }
}

function Test-UrlReachable {
    param([string]$Url)

    try {
        $response = Invoke-WebRequest -Uri $Url -Method Head -MaximumRedirection 5 -UseBasicParsing -TimeoutSec 20
        return $response.StatusCode -ge 200 -and $response.StatusCode -lt 400
    }
    catch {
        return $false
    }
}

function Get-LocalBootstrapper {
    param(
        [string]$InstallerDirectory,
        [string]$RequestedVersion,
        [string]$RequestedEdition
    )

    $candidates = @()

    if ($RequestedVersion -eq "2022") {
        $candidates = @(
            "vs_professional.exe",
            "vs_enterprise.exe",
            "vs_2022.exe",
            "vs2022.exe"
        )
    }
    else {
        $candidates = @(
            "vs_professional.exe",
            "vs_enterprise.exe",
            "vs_2026.exe",
            "vs2026.exe"
        )
    }

    foreach ($candidate in $candidates) {
        $candidatePath = Join-Path $InstallerDirectory $candidate
        if (Test-Path $candidatePath) {
            return $candidatePath
        }
    }

    return $null
}

if (-not (Test-IsAdministrator)) {
    Write-Error "Administrator rights required."
    exit 1
}

if ($Version -eq "2022") {
    $configFileName = "VS2022.vsconfig"
    $bootstrapperUrl = if ($Edition -eq "Professional") { "https://aka.ms/vs/17/release/vs_professional.exe" } else { "https://aka.ms/vs/17/release/vs_enterprise.exe" }
}
else {
    $configFileName = "vs2026.vsconfig"
    $bootstrapperUrl = if ($Edition -eq "Professional") { "https://aka.ms/vs/18/stable/vs_professional.exe" } else { "https://aka.ms/vs/18/stable/vs_enterprise.exe" }
}

$configPath = Join-Path $appRoot "Configs\$configFileName"
if (-not (Test-Path $configPath)) {
    Write-Error "Config file missing: $configPath"
    exit 1
}

if (-not (Test-Path $installerDir)) {
    Write-Error "Installer directory missing: $installerDir"
    exit 1
}

$localBootstrapper = Get-LocalBootstrapper -InstallerDirectory $installerDir -RequestedVersion $Version -RequestedEdition $Edition
$bootstrapperPath = $null
$downloadedBootstrapper = $false

Write-Output "----------------------------------------"
Write-Output "Visual Studio installation started"
Write-Output "----------------------------------------"
Write-Output "Version:       $Version"
Write-Output "Edition:       $Edition"
Write-Output "Config file:    $configPath"
Write-Output "Installer dir:  $installerDir"
Write-Output "Bootstrapper URL: $bootstrapperUrl"
Write-Output "----------------------------------------"

if ($localBootstrapper) {
    $bootstrapperPath = $localBootstrapper
    Write-Output "INFO: Using local installer: $bootstrapperPath"
}
else {
    if (-not (Test-InternetConnectivity)) {
        Write-Error "Internet unavailable and no local Visual Studio installer was found."
        exit 1
    }

    if (-not (Test-UrlReachable -Url $bootstrapperUrl)) {
        Write-Error "Bootstrapper download failed: URL not reachable."
        exit 1
    }

    $bootstrapperPath = Join-Path $tempDir "vs_${Version}_${Edition}_bootstrapper.exe"

    Write-Output "INFO: Downloading Visual Studio bootstrapper..."
    try {
        Invoke-WebRequest -Uri $bootstrapperUrl -OutFile $bootstrapperPath
        $downloadedBootstrapper = $true
    }
    catch {
        Write-Error "Installer download failed: $($_.Exception.Message)"
        exit 1
    }

    if (-not (Test-Path $bootstrapperPath)) {
        Write-Error "Installer download failed: bootstrapper file was not created."
        exit 1
    }
}

if (-not (Test-Path $bootstrapperPath)) {
    Write-Error "Installer file missing: $bootstrapperPath"
    exit 1
}

$installPath = Join-Path "C:\Program Files\Microsoft Visual Studio" "$Version\$Edition"
Write-Output "Install path:   $installPath"
Write-Output "----------------------------------------"
Write-Output "Starting Visual Studio silent installation..."

$arguments = @(
    "--quiet",
    "--wait",
    "--norestart",
    "--installPath", "`"$installPath`"",
    "--config", "`"$configPath`""
)

$process = Start-Process -FilePath $bootstrapperPath -ArgumentList $arguments -Wait -PassThru
Write-Output "Visual Studio installer exit code: $($process.ExitCode)"

if ($downloadedBootstrapper -and (Test-Path $bootstrapperPath)) {
    Remove-Item $bootstrapperPath -Force -ErrorAction SilentlyContinue
}

if ($process.ExitCode -ne 0) {
    Write-Host "Visual Studio Installation Failed" -ForegroundColor Red
    if (-not (Test-IsAdministrator)) {
        Write-Host "Reason: Administrator rights required" -ForegroundColor Red
    }
    elseif (-not (Test-Path $configPath)) {
        Write-Host "Reason: Config file missing" -ForegroundColor Red
    }
    elseif (-not $localBootstrapper -and -not (Test-InternetConnectivity)) {
        Write-Host "Reason: Internet unavailable" -ForegroundColor Red
    }
    else {
        Write-Host "Reason: Installer exited with code $($process.ExitCode)" -ForegroundColor Red
    }

    exit $process.ExitCode
}

Write-Output "----------------------------------------"
Write-Output "Visual Studio $Version $Edition installation completed successfully."
Write-Output "----------------------------------------"
exit 0
