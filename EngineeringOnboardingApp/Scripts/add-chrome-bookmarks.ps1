Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$appRoot = Split-Path -Parent $PSScriptRoot
$resourcesPath = Join-Path $appRoot "Data\resources.json"
$userDataRoot = Join-Path $env:LOCALAPPDATA "Google\Chrome\User Data"

function Get-ChromeTimestamp {
    $unixMicroseconds = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() * 1000
    $windowsEpochOffsetMicroseconds = 11644473600000000
    return ($unixMicroseconds + $windowsEpochOffsetMicroseconds).ToString()
}

function Get-ChromeProfiles {
    $profiles = New-Object System.Collections.Generic.List[string]

    if (-not (Test-Path $userDataRoot)) {
        return $profiles
    }

    $preferredProfiles = @("Default", "Profile 1", "Profile 2", "Profile 3", "Guest Profile")

    foreach ($profileName in $preferredProfiles) {
        $profilePath = Join-Path $userDataRoot $profileName
        if (Test-Path $profilePath) {
            $profiles.Add($profilePath) | Out-Null
        }
    }

    $additionalProfiles = Get-ChildItem -Path $userDataRoot -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match '^Profile \d+$' -and $_.FullName -notin $profiles }

    foreach ($profile in $additionalProfiles) {
        $profiles.Add($profile.FullName) | Out-Null
    }

    return $profiles
}

function Test-ChromeRunning {
    return [bool](Get-Process -Name chrome -ErrorAction SilentlyContinue)
}

function Initialize-BookmarksFile {
    param(
        [string]$FilePath
    )

    $directory = Split-Path -Parent $FilePath

    if (-not (Test-Path $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    if (-not (Test-Path $FilePath)) {
        $initial = [PSCustomObject]@{
            checksum = ""
            roots = [PSCustomObject]@{
                bookmark_bar = [PSCustomObject]@{
                    children = @()
                    date_added = (Get-ChromeTimestamp)
                    date_modified = (Get-ChromeTimestamp)
                    id = "1"
                    name = "Bookmarks bar"
                    type = "folder"
                }
                other = [PSCustomObject]@{
                    children = @()
                    date_added = (Get-ChromeTimestamp)
                    date_modified = (Get-ChromeTimestamp)
                    id = "2"
                    name = "Other bookmarks"
                    type = "folder"
                }
                synced = [PSCustomObject]@{
                    children = @()
                    date_added = (Get-ChromeTimestamp)
                    date_modified = (Get-ChromeTimestamp)
                    id = "3"
                    name = "Mobile bookmarks"
                    type = "folder"
                }
            }
            version = 1
        }

        $initial | ConvertTo-Json -Depth 30 | Set-Content -Path $FilePath -Encoding UTF8
    }
}

function Get-BookmarkBarChildren {
    param([object]$ChromeJson)

    if (-not $ChromeJson.roots.bookmark_bar.children) {
        $ChromeJson.roots.bookmark_bar | Add-Member -MemberType NoteProperty -Name children -Value @() -Force
    }

    return $ChromeJson.roots.bookmark_bar.children
}

function Test-BookmarkExists {
    param(
        [object[]]$Children,
        [string]$Url,
        [string]$Name
    )

    return [bool]($Children | Where-Object {
        $_.type -eq "url" -and (
            ($_.url -eq $Url) -or
            ($_.name -eq $Name)
        )
    })
}

if (-not (Test-Path $resourcesPath)) {
    Write-Error "resources.json not found: $resourcesPath"
    exit 1
}

if (Test-ChromeRunning) {
    Write-Error "Chrome is running. Close Chrome before adding bookmarks so the file can be updated safely."
    exit 1
}

$profiles = Get-ChromeProfiles
if ($profiles.Count -eq 0) {
    Write-Error "No Chrome profiles were found under: $userDataRoot"
    exit 1
}

Write-Output "INFO: Chrome profile(s) detected:"
foreach ($profile in $profiles) {
    Write-Output "INFO: Chrome profile detected: $(Split-Path -Leaf $profile)"
}

Write-Output "Loading resources from: $resourcesPath"
$resources = Get-Content $resourcesPath -Raw | ConvertFrom-Json

$bookmarkResources = @($resources | Where-Object {
    $_.AddToBookmarks -eq $true -and -not [string]::IsNullOrWhiteSpace($_.Url)
})

if ($bookmarkResources.Count -eq 0) {
    Write-Output "INFO: No bookmark-enabled resources found."
    exit 0
}

$verificationFailures = New-Object System.Collections.Generic.List[string]

foreach ($profilePath in $profiles) {
    $bookmarkFile = Join-Path $profilePath "Bookmarks"
    Initialize-BookmarksFile -FilePath $bookmarkFile

    Write-Output "Loading Chrome bookmarks from: $bookmarkFile"
    $json = Get-Content $bookmarkFile -Raw | ConvertFrom-Json
    $children = Get-BookmarkBarChildren -ChromeJson $json

    foreach ($item in $bookmarkResources) {
        $exists = Test-BookmarkExists -Children $children -Url $item.Url -Name $item.Name

        if (-not $exists) {
            $newBookmark = [PSCustomObject]@{
                date_added = (Get-ChromeTimestamp)
                guid = ([Guid]::NewGuid().ToString())
                id = (Get-Random -Minimum 100000 -Maximum 999999).ToString()
                name = $item.Name
                type = "url"
                url = $item.Url
            }

            $json.roots.bookmark_bar.children += $newBookmark
            Write-Output "INFO: Bookmark added: $($item.Name)"
        }
        else {
            Write-Output "INFO: Bookmark already exists: $($item.Name)"
        }
    }

    $json.roots.bookmark_bar.date_modified = Get-ChromeTimestamp
    $json | ConvertTo-Json -Depth 50 | Set-Content -Path $bookmarkFile -Encoding UTF8

    $verifyJson = Get-Content $bookmarkFile -Raw | ConvertFrom-Json
    $verifyChildren = Get-BookmarkBarChildren -ChromeJson $verifyJson

    foreach ($item in $bookmarkResources) {
        if (Test-BookmarkExists -Children $verifyChildren -Url $item.Url -Name $item.Name) {
            Write-Output "INFO: Verification passed: $($item.Name)"
        }
        else {
            $message = "Verification failed: $($item.Name) in profile $(Split-Path -Leaf $profilePath)"
            Write-Output "ERROR: $message"
            $verificationFailures.Add($message) | Out-Null
        }
    }
}

if ($verificationFailures.Count -gt 0) {
    Write-Error ($verificationFailures -join [Environment]::NewLine)
    exit 1
}

Write-Output "INFO: Chrome bookmarks updated successfully."
exit 0
