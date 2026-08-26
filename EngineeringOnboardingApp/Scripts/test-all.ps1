Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"

$ProjectRoot = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Get-Location
}

Set-Location $ProjectRoot

$Results = New-Object System.Collections.Generic.List[object]

function Is-Blank {
    param([string]$Text)

    if ($null -eq $Text) {
        return $true
    }

    return ($Text.Trim().Length -eq 0)
}

function Add-Result {
    param(
        [string]$Area,
        [string]$Item,
        [string]$Status,
        [string]$Message
    )

    $Results.Add([PSCustomObject]@{
        Area = $Area
        Item = $Item
        Status = $Status
        Message = $Message
    })

    if ($Status -eq "OK") {
        Write-Host "[OK] [$Area] $Item - $Message" -ForegroundColor Green
    }
    elseif ($Status -eq "WARN") {
        Write-Host "[WARN] [$Area] $Item - $Message" -ForegroundColor Yellow
    }
    else {
        Write-Host "[ERROR] [$Area] $Item - $Message" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "====================================================" -ForegroundColor Cyan
Write-Host " Engineering Onboarding App - Full Safe Test" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "Project root: $ProjectRoot"
Write-Host ""

# ----------------------------------------------------
# 1. PowerShell syntax check
# ----------------------------------------------------
Write-Host ""
Write-Host "=== 1. PowerShell script syntax check ===" -ForegroundColor Cyan

$scriptFiles = Get-ChildItem ".\Scripts" -Filter "*.ps1" -ErrorAction SilentlyContinue

if (-not $scriptFiles) {
    Add-Result "Scripts" "Scripts folder" "ERROR" "No PowerShell scripts found."
}
else {
    foreach ($script in $scriptFiles) {
        try {
            $content = Get-Content $script.FullName -Raw
            [System.Management.Automation.ScriptBlock]::Create($content) | Out-Null
            Add-Result "Scripts" $script.Name "OK" "Syntax valid."
        }
        catch {
            Add-Result "Scripts" $script.Name "ERROR" $_.Exception.Message
        }
    }
}

# ----------------------------------------------------
# 2. JSON validation
# ----------------------------------------------------
Write-Host ""
Write-Host "=== 2. JSON validation ===" -ForegroundColor Cyan

$jsonFiles = @(
    ".\Data\steps.json",
    ".\Data\tools.json",
    ".\Data\resources.json",
    ".\Configs\vs2026.vsconfig",
    ".\Configs\VS2022.vsconfig"
)

foreach ($file in $jsonFiles) {
    if (-not (Test-Path $file)) {
        Add-Result "JSON" $file "ERROR" "File missing."
        continue
    }

    try {
        Get-Content $file -Raw | ConvertFrom-Json | Out-Null
        Add-Result "JSON" $file "OK" "Valid JSON."
    }
    catch {
        Add-Result "JSON" $file "ERROR" $_.Exception.Message
    }
}

# ----------------------------------------------------
# 3. Required files check
# ----------------------------------------------------
Write-Host ""
Write-Host "=== 3. Required files check ===" -ForegroundColor Cyan

$requiredFiles = @(
    ".\Data\steps.json",
    ".\Data\tools.json",
    ".\Data\resources.json",
    ".\Configs\vs2026.vsconfig",
    ".\Configs\VS2022.vsconfig",
    ".\Configs\Installers\dotnet-sdk-10.0.301-win-x64.exe",
    ".\Configs\Installers\winsdksetup.exe",
    ".\Configs\Registry\hkcu.reg.txt",
    ".\Configs\Registry\hklm.reg.txt",
    ".\Assets\AppIcon.png",
    ".\Assets\AppIcon.ico",
    ".\EngineeringOnboardingApp.csproj",
    ".\MainWindow.xaml",
    ".\MainWindow.xaml.cs",
    ".\App.xaml",
    ".\App.xaml.cs"
)

foreach ($file in $requiredFiles) {
    if (Test-Path $file) {
        Add-Result "Files" $file "OK" "Found."
    }
    else {
        Add-Result "Files" $file "WARN" "Missing. This may be OK if not used yet."
    }
}

# ----------------------------------------------------
# 4. steps.json script reference check
# ----------------------------------------------------
Write-Host ""
Write-Host "=== 4. steps.json script reference check ===" -ForegroundColor Cyan

try {
    $steps = Get-Content ".\Data\steps.json" -Raw | ConvertFrom-Json

    foreach ($step in $steps) {
        if (-not (Is-Blank $step.scriptPath)) {
            $scriptPath = Join-Path $ProjectRoot $step.scriptPath

            if (Test-Path $scriptPath) {
                Add-Result "Steps" $step.id "OK" "Script exists: $($step.scriptPath)"
            }
            else {
                Add-Result "Steps" $step.id "ERROR" "Script missing: $($step.scriptPath)"
            }
        }

        if ($step.actionType -eq "OpenUrl" -or $step.actionType -eq "OpenSettings") {
            if (Is-Blank $step.url) {
                Add-Result "Steps" $step.id "ERROR" "URL action has empty URL."
            }
            else {
                Add-Result "Steps" $step.id "OK" "URL present: $($step.url)"
            }
        }
    }
}
catch {
    Add-Result "Steps" "steps.json" "ERROR" $_.Exception.Message
}

# ----------------------------------------------------
# 5. tools.json script reference check
# ----------------------------------------------------
Write-Host ""
Write-Host "=== 5. tools.json script reference check ===" -ForegroundColor Cyan

try {
    $tools = Get-Content ".\Data\tools.json" -Raw | ConvertFrom-Json

    foreach ($tool in $tools) {
        if (-not (Is-Blank $tool.scriptPath)) {
            $scriptPath = Join-Path $ProjectRoot $tool.scriptPath

            if (Test-Path $scriptPath) {
                Add-Result "Tools" $tool.id "OK" "Script exists: $($tool.scriptPath)"
            }
            else {
                Add-Result "Tools" $tool.id "ERROR" "Script missing: $($tool.scriptPath)"
            }
        }

        if ($tool.actionType -eq "OpenUrl" -or $tool.actionType -eq "OpenSettings") {
            if (Is-Blank $tool.url) {
                Add-Result "Tools" $tool.id "ERROR" "URL action has empty URL."
            }
            else {
                Add-Result "Tools" $tool.id "OK" "URL present: $($tool.url)"
            }
        }
    }
}
catch {
    Add-Result "Tools" "tools.json" "ERROR" $_.Exception.Message
}

# ----------------------------------------------------
# 6. System command checks
# ----------------------------------------------------
Write-Host ""
Write-Host "=== 6. System command checks ===" -ForegroundColor Cyan

$commands = @("powershell", "reg", "winget", "dotnet", "git")

foreach ($command in $commands) {
    $cmd = Get-Command $command -ErrorAction SilentlyContinue

    if ($cmd) {
        Add-Result "Commands" $command "OK" "Found: $($cmd.Source)"
    }
    else {
        if ($command -eq "git") {
            Add-Result "Commands" $command "WARN" "Not found. It may be installed by the app later."
        }
        else {
            Add-Result "Commands" $command "WARN" "Not found."
        }
    }
}

# ----------------------------------------------------
# 7. Visual Studio config preview
# ----------------------------------------------------
Write-Host ""
Write-Host "=== 7. Visual Studio config preview ===" -ForegroundColor Cyan

$vsConfigs = @(
    @{ Name = "Visual Studio 2026"; File = ".\Configs\vs2026.vsconfig"; Args = "-Version 2026 -Edition Professional" },
    @{ Name = "Visual Studio 2022"; File = ".\Configs\VS2022.vsconfig"; Args = "-Version 2022 -Edition Professional" }
)

foreach ($vs in $vsConfigs) {
    if (Test-Path $vs.File) {
        try {
            $config = Get-Content $vs.File -Raw | ConvertFrom-Json
            $count = 0

            if ($config.components) {
                $count = @($config.components).Count
            }

            Add-Result "VisualStudio" $vs.Name "OK" "Config found. Component count: $count. Would run: install-visualstudio.ps1 $($vs.Args)"
        }
        catch {
            Add-Result "VisualStudio" $vs.Name "ERROR" "Config invalid: $($_.Exception.Message)"
        }
    }
    else {
        Add-Result "VisualStudio" $vs.Name "ERROR" "Config missing: $($vs.File)"
    }
}

# ----------------------------------------------------
# 8. Chrome bookmark preview
# ----------------------------------------------------
Write-Host ""
Write-Host "=== 8. Chrome bookmark preview ===" -ForegroundColor Cyan

try {
    $resources = Get-Content ".\Data\resources.json" -Raw | ConvertFrom-Json

    $bookmarkResources = $resources | Where-Object {
        $_.AddToBookmarks -eq $true -and -not (Is-Blank $_.Url)
    }

    if (@($bookmarkResources).Count -eq 0) {
        Add-Result "Bookmarks" "resources.json" "WARN" "No bookmark-enabled resources found."
    }
    else {
        foreach ($item in $bookmarkResources) {
            Add-Result "Bookmarks" $item.Name "OK" "Would add: $($item.Url)"
        }
    }

    $bookmarkFile = Join-Path $env:LOCALAPPDATA "Google\Chrome\User Data\Default\Bookmarks"

    if (Test-Path $bookmarkFile) {
        Add-Result "Bookmarks" "Chrome Bookmarks file" "OK" "Found: $bookmarkFile"
    }
    else {
        Add-Result "Bookmarks" "Chrome Bookmarks file" "WARN" "Not found yet. Open Chrome once before running bookmark script."
    }
}
catch {
    Add-Result "Bookmarks" "Preview" "ERROR" $_.Exception.Message
}

# ----------------------------------------------------
# 9. Registry preview
# ----------------------------------------------------
Write-Host ""
Write-Host "=== 9. Registry preview ===" -ForegroundColor Cyan

try {
    $registryScript = ".\Scripts\apply-registry-files.ps1"

    if (Test-Path $registryScript) {
        powershell.exe -NoProfile -ExecutionPolicy Bypass -File $registryScript -PreviewOnly

        if ($LASTEXITCODE -eq 0) {
            Add-Result "Registry" "apply-registry-files.ps1" "OK" "Preview completed."
        }
        else {
            Add-Result "Registry" "apply-registry-files.ps1" "ERROR" "Preview command failed. Exit code: $LASTEXITCODE"
        }
    }
    else {
        Add-Result "Registry" "apply-registry-files.ps1" "ERROR" "Script missing."
    }
}
catch {
    Add-Result "Registry" "Preview" "ERROR" $_.Exception.Message
}

# ----------------------------------------------------
# 10. dotnet build check
# ----------------------------------------------------
Write-Host ""
Write-Host "=== 10. dotnet build check ===" -ForegroundColor Cyan

try {
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue

    if ($dotnet) {
        dotnet build ".\EngineeringOnboardingApp.csproj" --no-restore

        if ($LASTEXITCODE -eq 0) {
            Add-Result "Build" "dotnet build" "OK" "Build succeeded."
        }
        else {
            Add-Result "Build" "dotnet build" "ERROR" "Build failed. Exit code: $LASTEXITCODE"
        }
    }
    else {
        Add-Result "Build" "dotnet" "WARN" "dotnet command not found. Skipping build."
    }
}
catch {
    Add-Result "Build" "dotnet build" "ERROR" $_.Exception.Message
}

# ----------------------------------------------------
# 11. Hardcoded user/project path scan
# ----------------------------------------------------
Write-Host ""
Write-Host "=== 11. Hardcoded path scan ===" -ForegroundColor Cyan

$scanFiles = Get-ChildItem -Path ".\" -Recurse -Include "*.ps1","*.json","*.cs","*.xaml","*.csproj" -File |
    Where-Object {
        $_.FullName -notmatch "\\bin\\" -and
        $_.FullName -notmatch "\\obj\\" -and
        $_.FullName -notmatch "\\Logs\\"
    }

$hardcodedMatches = New-Object System.Collections.Generic.List[object]

$literalSearches = @(
    ("C:" + "\" + "Users" + "\"),
    ("550" + "026842"),
    ("source" + "\" + "repos" + "\" + "EngineeringOnboardingApp")
)

foreach ($file in $scanFiles) {
    try {
        $lines = Get-Content -Path $file.FullName -ErrorAction Stop

        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = [string]$lines[$i]

            foreach ($searchText in $literalSearches) {
                if ($line.Contains($searchText)) {
                    $hardcodedMatches.Add([PSCustomObject]@{
                        FilePath = $file.FullName
                        LineNumber = $i + 1
                        Line = $line
                    }) | Out-Null
                }
            }
        }
    }
    catch {
        Add-Result "HardcodedPaths" $file.FullName "WARN" "Could not scan file: $($_.Exception.Message)"
    }
}

if ($hardcodedMatches.Count -eq 0) {
    Add-Result "HardcodedPaths" "Project files" "OK" "No user-specific project paths found."
}
else {
    foreach ($match in $hardcodedMatches) {
        Add-Result "HardcodedPaths" $match.FilePath "WARN" "Line $($match.LineNumber): $($match.Line.Trim())"
    }
}

# ----------------------------------------------------
# Summary
# ----------------------------------------------------
Write-Host ""
Write-Host "====================================================" -ForegroundColor Cyan
Write-Host " Summary" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

$okCount = @($Results | Where-Object { $_.Status -eq "OK" }).Count
$warnCount = @($Results | Where-Object { $_.Status -eq "WARN" }).Count
$errorCount = @($Results | Where-Object { $_.Status -eq "ERROR" }).Count

Write-Host "OK:     $okCount" -ForegroundColor Green
Write-Host "WARN:   $warnCount" -ForegroundColor Yellow
Write-Host "ERROR:  $errorCount" -ForegroundColor Red

Write-Host ""

if ($errorCount -gt 0) {
    Write-Host "Validation failed. See [ERROR] entries above." -ForegroundColor Red
}
elseif ($warnCount -gt 0) {
    Write-Host "Validation completed with warnings. See [WARN] entries above." -ForegroundColor Yellow
}
else {
    Write-Host "Validation passed." -ForegroundColor Green
}

if (-not (Test-Path ".\Logs")) {
    New-Item -ItemType Directory -Path ".\Logs" -Force | Out-Null
}

$reportTimestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportPath = ".\Logs\full-safe-test-report-$reportTimestamp.csv"

$Results | Export-Csv -Path $reportPath -NoTypeInformation

Write-Host ""
Write-Host "Report saved to: $reportPath" -ForegroundColor Cyan