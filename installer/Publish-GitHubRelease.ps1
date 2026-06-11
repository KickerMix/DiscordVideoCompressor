param(
    [Parameter(Mandatory = $true)]
    [string]$Tag,
    [string]$Repository = "KickerMix/DiscordVideoCompressor",
    [string]$ReleaseName,
    [string]$InstallerPath,
    [string]$AppCastDirectory = ".\appcast",
    [string]$NotesFile,
    [switch]$Draft,
    [switch]$PreRelease
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$installerFullPath = $null
$installerPathProvided = -not [string]::IsNullOrWhiteSpace($InstallerPath)
if ($installerPathProvided) {
    $installerFullPath = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot $InstallerPath))
}
$appCastDirectoryFullPath = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot $AppCastDirectory))
$notesFileFullPath = if ($NotesFile) { [System.IO.Path]::GetFullPath((Join-Path $scriptRoot $NotesFile)) } else { $null }

if (-not $installerPathProvided) {
    $installerCandidate = Get-ChildItem -Path (Join-Path $scriptRoot "output") -Filter "DiscordVideoCompressor-Setup-$Tag.exe" | Select-Object -First 1
    if ($installerCandidate) {
        $installerFullPath = $installerCandidate.FullName
    }
}

if ([string]::IsNullOrWhiteSpace($installerFullPath) -or -not (Test-Path $installerFullPath)) {
    throw "Installer file not found: $installerFullPath"
}

if (-not (Test-Path $appCastDirectoryFullPath)) {
    throw "AppCast directory not found: $appCastDirectoryFullPath"
}

$appCastFiles = Get-ChildItem -Path $appCastDirectoryFullPath -File
if ($appCastFiles.Count -eq 0) {
    throw "No appcast files found in $appCastDirectoryFullPath"
}

$token = $env:GITHUB_TOKEN
if ([string]::IsNullOrWhiteSpace($token)) {
    throw "GITHUB_TOKEN is not set."
}

$headers = @{
    Authorization = "Bearer $token"
    Accept = "application/vnd.github+json"
    "X-GitHub-Api-Version" = "2022-11-28"
}

if ([string]::IsNullOrWhiteSpace($ReleaseName)) {
    $ReleaseName = $Tag
}

$releaseNotes = if ($notesFileFullPath) { Get-Content -Raw -Path $notesFileFullPath } else { "" }
$releaseBody = @{
    tag_name = $Tag
    name = $ReleaseName
    body = $releaseNotes
    draft = [bool]$Draft
    prerelease = [bool]$PreRelease
} | ConvertTo-Json

$repoApiBase = "https://api.github.com/repos/$Repository"

try {
    $release = Invoke-RestMethod -Method Get -Headers $headers -Uri "$repoApiBase/releases/tags/$Tag"
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -ne 404) {
        throw
    }

    $release = Invoke-RestMethod -Method Post -Headers $headers -ContentType "application/json" -Uri "$repoApiBase/releases" -Body $releaseBody
}

if ($release) {
    $release = Invoke-RestMethod -Method Patch -Headers $headers -ContentType "application/json" -Uri "$repoApiBase/releases/$($release.id)" -Body $releaseBody
}

$filesToUpload = @($installerFullPath) + ($appCastFiles.FullName)

foreach ($file in $filesToUpload) {
    $assetName = [System.IO.Path]::GetFileName($file)
    $existingAsset = $release.assets | Where-Object { $_.name -eq $assetName } | Select-Object -First 1
    if ($existingAsset) {
        Invoke-RestMethod -Method Delete -Headers $headers -Uri "$repoApiBase/releases/assets/$($existingAsset.id)" | Out-Null
    }

    $uploadHeaders = @{
        Authorization = "Bearer $token"
        Accept = "application/vnd.github+json"
        "Content-Type" = "application/octet-stream"
        "X-GitHub-Api-Version" = "2022-11-28"
    }

    $uploadUrl = "https://uploads.github.com/repos/$Repository/releases/$($release.id)/assets?name=$([Uri]::EscapeDataString($assetName))"
    Invoke-RestMethod -Method Post -Headers $uploadHeaders -Uri $uploadUrl -InFile $file | Out-Null
    Write-Host "Uploaded $assetName"
}

Write-Host "Release assets uploaded to https://github.com/$Repository/releases/tag/$Tag"
