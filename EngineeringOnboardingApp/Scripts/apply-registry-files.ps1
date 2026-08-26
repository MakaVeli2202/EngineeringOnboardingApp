param(
    [string]$HKCUFile = "",
    [string]$HKLMFile = "",
    [switch]$SkipHKCU,
    [switch]$SkipHKLM,
    [switch]$PreviewOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$appRoot = Split-Path -Parent $PSScriptRoot
$registryDir = Join-Path $appRoot "Configs\Registry"
$logsDir = Join-Path $appRoot "Logs"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupDir = Join-Path $logsDir "RegistryBackup_$timestamp"
$logFile = Join-Path $backupDir "registry-import-log.txt"
$logLines = New-Object System.Collections.Generic.List[string]

New-Item -ItemType Directory -Path $backupDir -Force | Out-Null

function Is-Blank {
    param([string]$Text)

    if ($null -eq $Text) {
        return $true
    }

    return ($Text.Trim().Length -eq 0)
}

function Write-Log {
    param(
        [string]$Message,
        [string]$Level = "INFO"
    )

    $line = "$(Get-Date -Format 'HH:mm:ss') [$Level] $Message"
    Write-Host $line
    [void]$logLines.Add($line)
}

function Save-LogFile {
    if ($logLines.Count -eq 0) {
        return
    }

    Set-Content -Path $logFile -Value $logLines -Encoding UTF8
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Test-RegFile {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        throw "Registry file not found: $Path"
    }

    $firstLine = Get-Content -Path $Path -TotalCount 1

    if ($firstLine -notmatch "Windows Registry Editor Version 5\.00") {
        throw "Invalid registry file header. Expected: Windows Registry Editor Version 5.00. File: $Path"
    }
}

function Test-RegistryKeyExists {
    param([string]$RegistryKey)

    & reg.exe query $RegistryKey *> $null
    return ($LASTEXITCODE -eq 0)
}

function Backup-RegistryKey {
    param(
        [string]$RegistryKey,
        [string]$BackupFileName
    )

    $backupPath = Join-Path $backupDir $BackupFileName

    if (Test-RegistryKeyExists -RegistryKey $RegistryKey) {
        Write-Log "Backing up registry key: $RegistryKey"
        Write-Log "Backup file: $backupPath"

        & reg.exe export $RegistryKey $backupPath /y *> $null

        if ($LASTEXITCODE -ne 0) {
            throw "Failed to export registry key: $RegistryKey"
        }

        Write-Log "Backup completed."
    }
    else {
        Write-Log "Registry key does not exist yet, backup skipped: $RegistryKey" "WARN"
    }
}

function Process-RegistryFile {
    param(
        [string]$ScopeName,
        [string]$SourceFile,
        [string]$RegistryKey,
        [string]$BackupFileName,
        [bool]$RequiresAdmin,
        [bool]$IsAdmin
    )

    Write-Log "Processing $ScopeName registry file."

    if (-not (Test-Path $SourceFile)) {
        Write-Log "$ScopeName file not found, skipping: $SourceFile" "WARN"
        return
    }

    if ($RequiresAdmin -and -not $IsAdmin) {
        Write-Log "$ScopeName requires Administrator rights. Skipping import." "WARN"
        return
    }

    Test-RegFile -Path $SourceFile

    $stagedFile = Join-Path $backupDir "$ScopeName`_import_$timestamp.reg"
    Copy-Item -Path $SourceFile -Destination $stagedFile -Force

    Write-Log "$ScopeName staged import file: $stagedFile"

    Backup-RegistryKey -RegistryKey $RegistryKey -BackupFileName $BackupFileName

    if ($PreviewOnly) {
        Write-Log "PreviewOnly enabled. Would import $ScopeName file: $stagedFile" "WARN"
        return
    }

    Write-Log "Importing $ScopeName registry file."

    & reg.exe import $stagedFile

    if ($LASTEXITCODE -ne 0) {
        throw "$ScopeName registry import failed with exit code $LASTEXITCODE"
    }

    Write-Log "$ScopeName registry import completed successfully."
}

try {
    if (Is-Blank $HKCUFile) {
        $HKCUFile = Join-Path $registryDir "hkcu.reg.txt"
    }

    if (Is-Blank $HKLMFile) {
        $HKLMFile = Join-Path $registryDir "hklm.reg.txt"
    }

    $isAdmin = Test-IsAdministrator

    Write-Log "----------------------------------------"
    Write-Log "Registry import safety check"
    Write-Log "----------------------------------------"
    Write-Log "App root: $appRoot"
    Write-Log "HKCU source: $HKCUFile"
    Write-Log "HKLM source: $HKLMFile"
    Write-Log "Backup directory: $backupDir"
    Write-Log "PreviewOnly: $PreviewOnly"
    Write-Log "SkipHKCU: $SkipHKCU"
    Write-Log "SkipHKLM: $SkipHKLM"
    Write-Log "Is Administrator: $isAdmin"
    Write-Log "----------------------------------------"

    if (-not $SkipHKCU) {
        Process-RegistryFile `
            -ScopeName "HKCU" `
            -SourceFile $HKCUFile `
            -RegistryKey "HKCU\Software\GE_Kretz" `
            -BackupFileName "backup_HKCU_GE_Kretz_$timestamp.reg" `
            -RequiresAdmin $false `
            -IsAdmin $isAdmin
    }
    else {
        Write-Log "SkipHKCU specified. HKCU skipped."
    }

    if (-not $SkipHKLM) {
        Process-RegistryFile `
            -ScopeName "HKLM" `
            -SourceFile $HKLMFile `
            -RegistryKey "HKLM\SOFTWARE\GE_Kretz" `
            -BackupFileName "backup_HKLM_GE_Kretz_$timestamp.reg" `
            -RequiresAdmin $true `
            -IsAdmin $isAdmin
    }
    else {
        Write-Log "SkipHKLM specified. HKLM skipped."
    }

    Write-Log "----------------------------------------"
    Write-Log "Registry import process finished."
    Write-Log "Backups/logs saved in: $backupDir"
    Write-Log "----------------------------------------"

    exit 0
}
catch {
    Write-Log "Registry import failed: $($_.Exception.Message)" "ERROR"
    Write-Log "Check backup/log folder: $backupDir" "ERROR"
    exit 1
}
finally {
    Save-LogFile
}
