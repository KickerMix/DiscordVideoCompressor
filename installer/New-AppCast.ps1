param(
    [Parameter(Mandatory = $true)]
    [string]$ReleaseFile,
    [string]$BaseUrl,
    [string]$GitHubRepository = "KickerMix/DiscordVideoCompressor",
    [string]$GitHubTag,
    [string]$Version,
    [string]$ProductName = "Discord Video Compressor",
    [string]$KeyPath = ".\keys",
    [string]$OutputDirectory = ".\appcast"
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$releaseFileFullPath = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot $ReleaseFile))
$outputDirectoryFullPath = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot $OutputDirectory))
$keyPathFullPath = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot $KeyPath))

if (-not (Test-Path $releaseFileFullPath)) {
    throw "Release file not found: $releaseFileFullPath"
}

if (-not (Get-Command "netsparkle-generate-appcast" -ErrorAction SilentlyContinue)) {
    throw "netsparkle-generate-appcast was not found. Install it with: dotnet tool install --global NetSparkleUpdater.Tools.AppCastGenerator"
}

if (-not $Version) {
    $versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($releaseFileFullPath)
    $Version = $versionInfo.ProductVersion
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    throw "Could not determine application version."
}

if ([string]::IsNullOrWhiteSpace($BaseUrl)) {
    if ([string]::IsNullOrWhiteSpace($GitHubTag)) {
        $GitHubTag = $Version
    }

    $BaseUrl = "https://github.com/$GitHubRepository/releases/download/$GitHubTag"
}

if (-not (Test-Path (Join-Path $keyPathFullPath "NetSparkle_Ed25519.pub")) -or -not (Test-Path (Join-Path $keyPathFullPath "NetSparkle_Ed25519.priv"))) {
    throw "NetSparkle keys were not found in $keyPathFullPath"
}

New-Item -ItemType Directory -Path $outputDirectoryFullPath -Force | Out-Null

Write-Host "Generating app cast for version $Version..."
netsparkle-generate-appcast `
    --single-file $releaseFileFullPath `
    --file-version $Version `
    --product-name $ProductName `
    --base-url $BaseUrl `
    --appcast-output-directory $outputDirectoryFullPath `
    --key-path $keyPathFullPath

if ($LASTEXITCODE -ne 0) {
    throw "netsparkle-generate-appcast failed with exit code $LASTEXITCODE"
}

Write-Host "App cast files created in $outputDirectoryFullPath"
