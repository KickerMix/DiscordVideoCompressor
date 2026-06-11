param(
    [string]$DownloadUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2026-06-10-17-02/ffmpeg-n8.1-latest-win64-gpl-8.1.zip",
    [string]$ExpectedArchiveSha256 = "D8227EB85F9327F4FCDF3CFBC84B8B14492C9DBB7C324B4DE32654F1CCDDD75E",
    [string]$ExpectedExecutableSha256 = "CA7E2992D973AA5B042BE093EDA6FDCE43CA41AAFDD647148278589B1D753F60"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$workingDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("DiscordVideoCompressor-ffmpeg-" + [guid]::NewGuid().ToString("N"))
$archivePath = Join-Path $workingDirectory "ffmpeg.zip"
$extractPath = Join-Path $workingDirectory "extract"

try {
    New-Item -ItemType Directory -Path $workingDirectory | Out-Null
    Invoke-WebRequest -UseBasicParsing -Uri $DownloadUrl -OutFile $archivePath

    $archiveHash = (Get-FileHash $archivePath -Algorithm SHA256).Hash
    if ($archiveHash -ne $ExpectedArchiveSha256) {
        throw "FFmpeg archive checksum mismatch. Expected $ExpectedArchiveSha256, got $archiveHash."
    }

    Expand-Archive -LiteralPath $archivePath -DestinationPath $extractPath
    $packageRoot = Get-ChildItem $extractPath -Directory | Select-Object -First 1
    if (-not $packageRoot) {
        throw "FFmpeg archive does not contain a package directory."
    }

    $ffmpegSource = Join-Path $packageRoot.FullName "bin\ffmpeg.exe"
    $licenseSource = Join-Path $packageRoot.FullName "LICENSE.txt"
    $executableHash = (Get-FileHash $ffmpegSource -Algorithm SHA256).Hash
    if ($executableHash -ne $ExpectedExecutableSha256) {
        throw "ffmpeg.exe checksum mismatch. Expected $ExpectedExecutableSha256, got $executableHash."
    }

    Copy-Item -LiteralPath $ffmpegSource -Destination (Join-Path $repoRoot "DiscordVideoCompressor\Resources\ffmpeg.exe") -Force
    Copy-Item -LiteralPath $licenseSource -Destination (Join-Path $repoRoot "FFMPEG-GPL-LICENSE.txt") -Force
    Write-Host "Updated embedded FFmpeg ($executableHash)."
}
finally {
    if (Test-Path $workingDirectory) {
        Remove-Item -LiteralPath $workingDirectory -Recurse -Force
    }
}
